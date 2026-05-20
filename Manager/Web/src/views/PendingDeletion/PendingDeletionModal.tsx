import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Button,
  Collapse,
  Empty,
  Flex,
  Modal,
  Popconfirm,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
  notification,
} from 'antd';
import {
  AppstoreOutlined,
  CloudServerOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  FlagOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import type {
  PendingApplicationItem,
  PendingFeatureItem,
} from './types';
import type { usePendingDeletion } from './usePendingDeletion';

const { Text, Title } = Typography;

type PendingState = ReturnType<typeof usePendingDeletion>;

interface PendingDeletionModalProps {
  open: boolean;
  onClose: () => void;
  pending: PendingState;
}

interface NodeGroup {
  nodeId: number;
  nodeName: string;
  features: PendingFeatureItem[];
  applications: PendingApplicationItem[];
}

function formatDateTime(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleString();
}

export function PendingDeletionModal({ open, onClose, pending }: PendingDeletionModalProps) {
  const { features, applications, totalCount, isLoading, error, refresh, deleteBatch } = pending;

  const [selectedFeatureKeys, setSelectedFeatureKeys] = useState<Record<number, React.Key[]>>({});
  const [selectedApplicationKeys, setSelectedApplicationKeys] = useState<Record<number, React.Key[]>>({});
  const [isDeleting, setIsDeleting] = useState(false);

  // Refresh once whenever the modal opens — the page-level hook only polls hourly.
  useEffect(() => {
    if (open) {
      void refresh();
    }
  }, [open, refresh]);

  const grouped: NodeGroup[] = useMemo(() => {
    const map = new Map<number, NodeGroup>();
    for (const f of features) {
      if (!map.has(f.nodeId)) {
        map.set(f.nodeId, { nodeId: f.nodeId, nodeName: f.nodeName, features: [], applications: [] });
      }
      map.get(f.nodeId)!.features.push(f);
    }
    for (const a of applications) {
      if (!map.has(a.nodeId)) {
        map.set(a.nodeId, { nodeId: a.nodeId, nodeName: a.nodeName, features: [], applications: [] });
      }
      map.get(a.nodeId)!.applications.push(a);
    }
    return Array.from(map.values()).sort((x, y) => x.nodeName.localeCompare(y.nodeName));
  }, [features, applications]);

  const selectedFeatures = useMemo(() => {
    return features.filter((f) =>
      (selectedFeatureKeys[f.nodeId] ?? []).includes(`${f.applicationName}::${f.featureName}`));
  }, [features, selectedFeatureKeys]);

  const selectedApplications = useMemo(() => {
    return applications.filter((a) =>
      (selectedApplicationKeys[a.nodeId] ?? []).includes(a.applicationName));
  }, [applications, selectedApplicationKeys]);

  const selectedCount = selectedFeatures.length + selectedApplications.length;

  const handleConfirmDelete = async () => {
    setIsDeleting(true);
    try {
      const summary = await deleteBatch(selectedFeatures, selectedApplications);
      const restoredCount = summary.failures.filter((f) => f.reason === 'restored').length;
      const errorCount = summary.failures.filter((f) => f.reason !== 'restored').length;

      if (summary.successCount > 0) {
        notification.success({
          message: `Deleted ${summary.successCount} item${summary.successCount === 1 ? '' : 's'}`,
        });
      }
      if (restoredCount > 0) {
        notification.info({
          message: `${restoredCount} item${restoredCount === 1 ? ' was' : 's were'} restored`,
          description: 'These items were used between view and confirmation, so they were skipped.',
        });
      }
      if (errorCount > 0) {
        notification.error({
          message: `${errorCount} deletion${errorCount === 1 ? '' : 's'} failed`,
          description: summary.failures
            .filter((f) => f.reason !== 'restored')
            .slice(0, 3)
            .map((f) => `${f.applicationName}${f.featureName ? `/${f.featureName}` : ''}: ${f.message ?? f.reason}`)
            .join('\n'),
        });
      }

      setSelectedFeatureKeys({});
      setSelectedApplicationKeys({});
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <Modal
      title={
        <Space>
          <ExclamationCircleOutlined style={{ color: '#faad14' }} />
          <span>Flags Pending Permanent Deletion</span>
          {totalCount > 0 && <Tag color="warning">{totalCount}</Tag>}
        </Space>
      }
      open={open}
      onCancel={onClose}
      width={900}
      footer={
        <Flex justify="space-between" align="center">
          <Space>
            <Button icon={<ReloadOutlined spin={isLoading} />} onClick={refresh}>
              Refresh
            </Button>
            <Text type="secondary">{selectedCount} selected</Text>
          </Space>
          <Space>
            <Button onClick={onClose}>Close</Button>
            <Popconfirm
              title="Permanently delete selected items?"
              description="This cannot be undone. All usage history will be removed."
              onConfirm={handleConfirmDelete}
              okText="Delete"
              okButtonProps={{ danger: true, loading: isDeleting }}
              cancelText="Cancel"
              disabled={selectedCount === 0}
            >
              <Button
                type="primary"
                danger
                icon={<DeleteOutlined />}
                disabled={selectedCount === 0}
                loading={isDeleting}
              >
                Delete {selectedCount > 0 ? `(${selectedCount})` : ''}
              </Button>
            </Popconfirm>
          </Space>
        </Flex>
      }
    >
      {error && (
        <Alert type="error" showIcon style={{ marginBottom: 12 }} message={error} />
      )}

      <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
        These flags and applications have not been registered or read for the configured stale threshold.
        Reading them again from your application restores them automatically.
      </Text>

      {isLoading && totalCount === 0 ? (
        <Flex justify="center" align="center" style={{ padding: 48 }}>
          <Spin />
        </Flex>
      ) : totalCount === 0 ? (
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="Nothing pending deletion"
        />
      ) : (
        <Collapse
          defaultActiveKey={grouped.map((g) => `node-${g.nodeId}`)}
          items={grouped.map((g) => ({
            key: `node-${g.nodeId}`,
            label: (
              <Space>
                <CloudServerOutlined />
                <Text strong>{g.nodeName}</Text>
                <Tag>{g.features.length + g.applications.length}</Tag>
              </Space>
            ),
            children: (
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                {g.applications.length > 0 && (
                  <div>
                    <Title level={5} style={{ marginTop: 0 }}>
                      <Space><AppstoreOutlined /> Applications</Space>
                    </Title>
                    <Table<PendingApplicationItem>
                      size="small"
                      dataSource={g.applications}
                      rowKey={(r) => r.applicationName}
                      pagination={false}
                      rowSelection={{
                        selectedRowKeys: selectedApplicationKeys[g.nodeId] ?? [],
                        onChange: (keys) =>
                          setSelectedApplicationKeys((prev) => ({ ...prev, [g.nodeId]: keys })),
                      }}
                      columns={[
                        { title: 'Application', dataIndex: 'applicationName', key: 'app' },
                        { title: 'Last Used', key: 'lastUsed', render: (_, r) => formatDateTime(r.lastUsedAt) },
                        { title: 'Pending Since', key: 'since', render: (_, r) => formatDateTime(r.pendingDeletionSince) },
                      ]}
                    />
                  </div>
                )}
                {g.features.length > 0 && (
                  <div>
                    <Title level={5} style={{ marginTop: 0 }}>
                      <Space><FlagOutlined /> Features</Space>
                    </Title>
                    <Table<PendingFeatureItem>
                      size="small"
                      dataSource={g.features}
                      rowKey={(r) => `${r.applicationName}::${r.featureName}`}
                      pagination={false}
                      rowSelection={{
                        selectedRowKeys: selectedFeatureKeys[g.nodeId] ?? [],
                        onChange: (keys) =>
                          setSelectedFeatureKeys((prev) => ({ ...prev, [g.nodeId]: keys })),
                      }}
                      columns={[
                        { title: 'Application', dataIndex: 'applicationName', key: 'app' },
                        { title: 'Feature', dataIndex: 'featureName', key: 'feature',
                          render: (text) => <Text code>{text}</Text> },
                        { title: 'Last Used', key: 'lastUsed', render: (_, r) => formatDateTime(r.lastUsedAt) },
                        { title: 'Pending Since', key: 'since', render: (_, r) => formatDateTime(r.pendingDeletionSince) },
                      ]}
                    />
                  </div>
                )}
              </Space>
            ),
          }))}
        />
      )}
    </Modal>
  );
}
