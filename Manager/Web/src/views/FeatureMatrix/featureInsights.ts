// Helpers for feature details and read activity shared by the matrix and the details panel.
// Kept out of the component files so React Fast Refresh can update those in place.

import type { CellState, CellUsage, FeatureDetails, FeatureMatrixRow } from './types';

const DAY_MS = 86_400_000;

export function formatRelativeTime(iso: string): string {
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return iso;
  const diffMs = Date.now() - then;
  const minutes = Math.round(diffMs / 60_000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} hour${hours === 1 ? '' : 's'} ago`;
  const days = Math.round(hours / 24);
  if (days < 30) return `${days} day${days === 1 ? '' : 's'} ago`;
  const months = Math.round(days / 30);
  return `${months} month${months === 1 ? '' : 's'} ago`;
}

// ---- Details ----

export function cellDetails(cell?: CellState): FeatureDetails | undefined {
  return cell?.kind === 'value' ? cell.details : undefined;
}

export function hasContent(details?: FeatureDetails): details is FeatureDetails {
  return Boolean(details && (details.description || details.links.length > 0));
}

// Details of a row as registered on the first node that has any; used for the matrix hint.
export function rowDetails(row: Pick<FeatureMatrixRow, 'cells'>, nodeNames: string[]): FeatureDetails | undefined {
  for (const name of nodeNames) {
    const details = cellDetails(row.cells[name]);
    if (hasContent(details)) return details;
  }
  return undefined;
}

// ---- Read activity ----

// 'unknown': the node is too old to report reads; 'not-read-yet': no reads, but either registered
// within the warning period or the warning is disabled.
export type ReadStatus = 'read' | 'unread' | 'not-read-yet' | 'unknown';

function startOfUtcDay(time: number): number {
  const d = new Date(time);
  return Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate());
}

function daysSince(iso: string, now: number): number {
  return Math.max(0, Math.floor((startOfUtcDay(now) - startOfUtcDay(new Date(iso).getTime())) / DAY_MS));
}

function plural(count: number, word: string): string {
  return `${count} ${word}${count === 1 ? '' : 's'}`;
}

/**
 * @param warnAfterDays Manager setting; null when the unread warning is disabled.
 */
export function readStatus(usage: CellUsage, warnAfterDays: number | null, now = Date.now()): ReadStatus {
  if (!usage.registeredAt) return 'unknown';
  if (warnAfterDays === null) return usage.lastReadAt ? 'read' : 'not-read-yet';
  if (usage.lastReadAt && daysSince(usage.lastReadAt, now) < warnAfterDays) return 'read';
  return daysSince(usage.registeredAt, now) < warnAfterDays ? 'not-read-yet' : 'unread';
}

/** One line for tooltips and the details panel, or null when the node does not report reads. */
export function describeReads(usage: CellUsage, warnAfterDays: number | null, now = Date.now()): string | null {
  const status = readStatus(usage, warnAfterDays, now);
  if (status === 'unknown') return null;

  if (usage.lastReadAt) {
    const days = daysSince(usage.lastReadAt, now);
    return `Last read: ${days === 0 ? 'today' : days === 1 ? 'yesterday' : `${days} days ago`}`;
  }
  if (status === 'unread') return `No reads in the last ${plural(warnAfterDays!, 'day')}`;

  const registeredDays = daysSince(usage.registeredAt!, now);
  return registeredDays < (warnAfterDays ?? Infinity)
    ? `Registered ${registeredDays === 0 ? 'today' : `${plural(registeredDays, 'day')} ago`}, not read yet`
    : 'No reads recorded';
}

function valueCell(row: FeatureMatrixRow, nodeName: string) {
  const cell = row.cells[nodeName];
  return cell?.kind === 'value' ? cell : undefined;
}

/** Registered on at least one node where the application has not read it for the warning period. */
export function isUnreadOnAnyNode(row: FeatureMatrixRow, nodeNames: string[], warnAfterDays: number | null): boolean {
  if (warnAfterDays === null) return false;
  return nodeNames.some((nodeName) => {
    const cell = valueCell(row, nodeName);
    return cell !== undefined && readStatus(cell, warnAfterDays) === 'unread';
  });
}

/** Present on every node with the same state: likely fully rolled out (ON) or abandoned (OFF). */
export function sameStateEverywhere(row: FeatureMatrixRow, nodeNames: string[]): boolean | null {
  if (nodeNames.length < 2) return null;
  const cells = nodeNames.map((nodeName) => valueCell(row, nodeName));
  if (cells.some((cell) => cell === undefined)) return null;
  const first = cells[0]!.value;
  return cells.every((cell) => cell!.value === first) ? first : null;
}

// ---- Cleanup hints for the details panel ----

export interface CleanupHints {
  unreadOn: string[];
  notReadYetOn: { node: string; text: string }[];
  sameState: boolean | null;
}

export function cleanupHints(
  row: FeatureMatrixRow | undefined,
  nodeNames: string[],
  warnAfterDays: number | null,
): CleanupHints {
  const hints: CleanupHints = { unreadOn: [], notReadYetOn: [], sameState: null };
  if (!row) return hints;

  for (const nodeName of nodeNames) {
    const cell = valueCell(row, nodeName);
    if (!cell) continue;
    const status = readStatus(cell, warnAfterDays);
    if (status === 'unread') hints.unreadOn.push(nodeName);
    // Only worth explaining while the warning is enabled: why a feature without reads isn't flagged.
    if (status === 'not-read-yet' && warnAfterDays !== null) {
      hints.notReadYetOn.push({ node: nodeName, text: describeReads(cell, warnAfterDays) ?? '' });
    }
  }

  hints.sameState = sameStateEverywhere(row, nodeNames);
  return hints;
}
