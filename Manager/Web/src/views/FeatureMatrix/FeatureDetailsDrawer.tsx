import { useMemo, useState } from 'react';
import {
  Alert,
  Collapse,
  Descriptions,
  Drawer,
  Empty,
  Flex,
  Segmented,
  Space,
  Tag,
  Typography,
} from 'antd';
import {
  AppstoreOutlined,
  CheckOutlined,
  CloseOutlined,
  CloudServerOutlined,
  ExportOutlined,
  LinkOutlined,
} from '@ant-design/icons';

import type { CellState, FeatureDetails, FeatureMatrixRow, NodeDto } from './types';
import {
  cellDetails,
  cleanupHints,
  describeReads,
  formatRelativeTime,
  hasContent,
  readStatus,
  rowDetails,
  type CleanupHints,
} from './featureInsights';

// The same rules as the "Cleanup candidates" filters, explained for one feature.
function CleanupHintsSection({
  hints,
  nodeCount,
  warnAfterDays,
}: {
  hints: CleanupHints;
  nodeCount: number;
  warnAfterDays: number | null;
}) {
  if (hints.unreadOn.length === 0 && hints.sameState === null) {
    return null;
  }

  return (
    <section>
      <Title level={5} style={{ marginTop: 0 }}>Cleanup hints</Title>
      <Flex vertical gap={8}>
        {hints.unreadOn.length > 0 && (
          <Alert
            type="warning"
            showIcon
            title={`Not read in the last ${warnAfterDays} days on ${hints.unreadOn.length === nodeCount ? 'any node' : hints.unreadOn.join(', ')}`}
            description="The application registers this flag, but no code has read it there. It may be left over after a refactoring or never wired up — check the code and remove it if it is unused. A flag read only rarely (e.g. by a monthly job) can show up here too."
          />
        )}
        {hints.sameState !== null && (
          <Alert
            type="info"
            showIcon
            title={`${hints.sameState ? 'ON' : 'OFF'} on all ${nodeCount} nodes`}
            description={
              hints.sameState
                ? 'If the rollout is complete, the flag and the old code path can be removed from the application.'
                : 'If the feature was abandoned, the flag and the unused code path can be removed from the application.'
            }
          />
        )}
      </Flex>
    </section>
  );
}

const { Text, Paragraph, Title } = Typography;

const CLIENT_DOCS_URL =
  'https://github.com/Wookashi/FeatureSwitcher/blob/main/Client/README.md#describing-features';

// Defense in depth: the Manager API already drops other schemes, but never render one as a link.
function isHttpUrl(url: string): boolean {
  try {
    const { protocol } = new URL(url);
    return protocol === 'http:' || protocol === 'https:';
  } catch {
    return false;
  }
}

export interface FeatureDetailsTarget {
  application: string;
  feature: string;
}

interface FeatureDetailsDrawerProps {
  target: FeatureDetailsTarget | null;
  nodes: NodeDto[];
  rows: FeatureMatrixRow[];
  onClose: () => void;
  warnAfterDays: number | null; // null = unread warning disabled
}

function sameDetails(a?: FeatureDetails, b?: FeatureDetails): boolean {
  const key = (d?: FeatureDetails) =>
    hasContent(d) ? JSON.stringify([d.description ?? null, d.links.map((l) => [l.url, l.title ?? null])]) : '';
  return key(a) === key(b);
}

// A link without a title is shown as host + path, e.g. "jira.example.com/browse/SHOP-1423".
function labelFromUrl(url: string): string {
  try {
    const u = new URL(url);
    const path = u.pathname === '/' ? '' : u.pathname.replace(/\/$/, '');
    const label = `${u.host}${path}`;
    return label.length > 60 ? `${label.slice(0, 57)}…` : label;
  } catch {
    return url;
  }
}

const URL_PATTERN = /(https?:\/\/[^\s<>()]+[^\s<>().,;:!?'"])/g;

// Plain text only (never HTML): bare http(s) URLs become links.
function Linkified({ text }: { text: string }) {
  const parts = text.split(URL_PATTERN);
  return (
    <>
      {parts.map((part, i) =>
        i % 2 === 1 ? (
          <a key={i} href={part} target="_blank" rel="noopener noreferrer">{labelFromUrl(part)}</a>
        ) : (
          <span key={i}>{part}</span>
        ),
      )}
    </>
  );
}

// Minimal formatting that reads well as a plain string in C#:
// blank line = new paragraph, lines starting with "- " = bullet list, other line breaks are kept.
function FormattedDescription({ text }: { text: string }) {
  const blocks = text.trim().split(/\n\s*\n/);
  return (
    <div>
      {blocks.map((block, i) => {
        const lines = block.split('\n').map((l) => l.trim()).filter(Boolean);
        const isList = lines.every((l) => l.startsWith('- '));
        return isList ? (
          <ul key={i} style={{ margin: '0 0 8px 0', paddingLeft: 20 }}>
            {lines.map((l, j) => (
              <li key={j}><Linkified text={l.slice(2)} /></li>
            ))}
          </ul>
        ) : (
          <Paragraph key={i} style={{ whiteSpace: 'pre-line', marginBottom: 8 }}>
            <Linkified text={lines.join('\n')} />
          </Paragraph>
        );
      })}
    </div>
  );
}

function DetailsBody({ details }: { details?: FeatureDetails }) {
  if (!hasContent(details)) {
    return (
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description={
          <Space orientation="vertical" size={4}>
            <Text type="secondary">This application didn't provide a description.</Text>
            <Text type="secondary" style={{ fontSize: 12 }}>
              Developers can add one in code using <Text code>Description</Text> and <Text code>Links</Text> —{' '}
              <a href={CLIENT_DOCS_URL} target="_blank" rel="noopener noreferrer">see the client documentation</a>.
            </Text>
          </Space>
        }
      />
    );
  }

  const links = details.links.filter((link) => isHttpUrl(link.url));
  return (
    <Space orientation="vertical" size={12} style={{ width: '100%' }}>
      {details.description && <FormattedDescription text={details.description} />}
      {links.length > 0 && (
        <Flex vertical gap={6}>
          {links.map((link) => (
            <a key={link.url} href={link.url} target="_blank" rel="noopener noreferrer">
              <Space size={6}>
                <LinkOutlined />
                <span>{link.title ?? labelFromUrl(link.url)}</span>
                <ExportOutlined style={{ fontSize: 11, opacity: 0.6 }} />
              </Space>
            </a>
          ))}
        </Flex>
      )}
    </Space>
  );
}

function StateTag({ cell }: { cell?: CellState }) {
  if (cell?.kind !== 'value') {
    return <Tag style={{ margin: 0 }}>N/A</Tag>;
  }
  return (
    <Tag
      color={cell.value ? 'success' : 'error'}
      icon={cell.value ? <CheckOutlined /> : <CloseOutlined />}
      style={{ margin: 0 }}
    >
      {cell.value ? 'ON' : 'OFF'}
    </Tag>
  );
}

export function FeatureDetailsDrawer({ target, nodes, rows, onClose, warnAfterDays }: FeatureDetailsDrawerProps) {
  const [selectedNode, setSelectedNode] = useState<string | null>(null);

  const row = useMemo(
    () => (target ? rows.find((r) => r.application === target.application && r.feature === target.feature) : undefined),
    [rows, target],
  );

  // Nodes where this application has the feature registered, with the details each node reports.
  const perNode = useMemo(() => {
    if (!row) return [];
    return nodes
      .filter((n) => row.cells[n.name]?.kind === 'value')
      .map((n) => ({ node: n.name, details: cellDetails(row.cells[n.name]) }));
  }, [nodes, row]);

  const differs = perNode.some((p) => !sameDetails(p.details, perNode[0]?.details));
  const activeNode = selectedNode && perNode.some((p) => p.node === selectedNode) ? selectedNode : perNode[0]?.node;
  const activeDetails = perNode.find((p) => p.node === activeNode)?.details;

  // Other applications that registered a flag with the same name (shared flag).
  const otherApps = useMemo(() => {
    if (!target) return [];
    const apps = new Set(
      rows
        .filter((r) => r.feature === target.feature && r.application !== target.application)
        .map((r) => r.application),
    );
    return Array.from(apps).sort((a, b) => a.localeCompare(b));
  }, [rows, target]);

  return (
    <Drawer
      open={target !== null}
      onClose={() => {
        setSelectedNode(null);
        onClose();
      }}
      size={520}
      destroyOnHidden
      title={
        target && (
          <Flex vertical gap={2}>
            <Space size={8}>
              <Text code style={{ fontSize: 15 }}>{target.feature}</Text>
              {otherApps.length > 0 && <Tag color="blue" style={{ margin: 0 }}>Shared</Tag>}
            </Space>
            <Space size={6}>
              <AppstoreOutlined style={{ color: '#722ed1' }} />
              <Text type="secondary" style={{ fontWeight: 400 }}>{target.application}</Text>
            </Space>
          </Flex>
        )
      }
    >
      {target && (
        <Space orientation="vertical" size={24} style={{ width: '100%' }}>
          <CleanupHintsSection
            hints={cleanupHints(row, nodes.map((n) => n.name), warnAfterDays)}
            nodeCount={nodes.length}
            warnAfterDays={warnAfterDays}
          />
          <section>
            <Title level={5} style={{ marginTop: 0 }}>About</Title>
            {differs && (
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 12 }}
                title="Nodes report different details"
                description="Each node stores what the application registered there — e.g. an older app version on one environment."
              />
            )}
            {differs && (
              <Segmented
                style={{ marginBottom: 12 }}
                value={activeNode}
                onChange={(v) => setSelectedNode(String(v))}
                options={perNode.map((p) => ({ label: p.node, value: p.node }))}
              />
            )}
            <DetailsBody details={activeDetails} />
          </section>

          <section>
            <Title level={5}>State per node</Title>
            <Descriptions
              size="small"
              bordered
              column={1}
              items={nodes.map((n) => {
                const cell = row?.cells[n.name];
                const usage = cell?.kind === 'value' ? cell : undefined;
                return {
                  key: n.name,
                  label: (
                    <Space size={6}>
                      <CloudServerOutlined />
                      {n.name}
                    </Space>
                  ),
                  children: (
                    <Flex justify="space-between" align="center" gap={12}>
                      <StateTag cell={cell} />
                      <Text
                        type={usage && readStatus(usage, warnAfterDays) === 'unread' ? 'warning' : 'secondary'}
                        style={{ fontSize: 12 }}
                      >
                        {usage
                          ? (describeReads(usage, warnAfterDays) ?? (usage.lastUsedAt ? `used ${formatRelativeTime(usage.lastUsedAt)}` : ''))
                          : 'not registered'}
                        {typeof usage?.usesLast7Days === 'number' && ` · ${usage.usesLast7Days} read${usage.usesLast7Days === 1 ? '' : 's'} / 7d`}
                      </Text>
                    </Flex>
                  ),
                };
              })}
            />
            {/* Explains why a node without reads isn't flagged; only relevant while the warning is enabled. */}
            {warnAfterDays !== null &&
              nodes.some((n) => {
                const cell = row?.cells[n.name];
                return cell?.kind === 'value' && readStatus(cell, warnAfterDays) === 'not-read-yet';
              }) && (
                <Text type="secondary" style={{ display: 'block', marginTop: 8, fontSize: 12 }}>
                  Features are reported as unread only once they have been registered for {warnAfterDays} days without being read.
                </Text>
              )}
          </section>

          {otherApps.length > 0 && (
            <section>
              <Title level={5}>Also used by</Title>
              <Text type="secondary" style={{ display: 'block', marginBottom: 8, fontSize: 12 }}>
                Shared flag — changing its state affects all these applications on the node.
              </Text>
              <Collapse
                size="small"
                items={otherApps.map((app) => {
                  // Prefer the node selected above, so all applications are shown for the same environment.
                  const otherRow = rows.find((r) => r.application === app && r.feature === target.feature);
                  const details = otherRow
                    ? (cellDetails(otherRow.cells[activeNode ?? '']) ?? rowDetails(otherRow, nodes.map((n) => n.name)))
                    : undefined;
                  return {
                    key: app,
                    label: (
                      <Space size={6}>
                        <AppstoreOutlined style={{ color: '#722ed1' }} />
                        <Text strong>{app}</Text>
                        {!details?.description && <Text type="secondary" style={{ fontSize: 12 }}>(no description)</Text>}
                      </Space>
                    ),
                    children: <DetailsBody details={details} />,
                  };
                })}
              />
            </section>
          )}
        </Space>
      )}
    </Drawer>
  );
}
