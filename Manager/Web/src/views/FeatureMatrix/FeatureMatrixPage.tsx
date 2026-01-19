import { useMemo, useState } from 'react';
import {
  ConfigProvider,
  Layout,
  Table,
  Tag,
  Input,
  Button,
  Switch,
  Space,
  Tooltip,
  Alert,
  Spin,
  Typography,
  Card,
  Row,
  Col,
  Statistic,
  Divider,
  Flex,
  Badge,
  theme as antTheme,
} from 'antd';
import {
  CheckOutlined,
  CloseOutlined,
  QuestionOutlined,
  ReloadOutlined,
  SunOutlined,
  MoonOutlined,
  LoadingOutlined,
  AppstoreOutlined,
  FlagOutlined,
  CloudServerOutlined,
  SearchOutlined,
  FilterOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import { useFeatureMatrix } from './useFeatureMatrix';
import { useTheme } from './theme';
import type { FeatureMatrixRow, CellState, NodeDto } from './types';

const { Header, Content } = Layout;
const { Title, Text } = Typography;

interface CellRendererProps {
  state: CellState | undefined;
  onClick?: () => void;
}

function CellRenderer({ state, onClick }: CellRendererProps) {
  if (!state || state.kind === 'loading') {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 32 }}>
        <Spin size="small" indicator={<LoadingOutlined spin />} />
      </Flex>
    );
  }

  if (state.kind === 'unknown') {
    return (
      <Flex justify="center">
        <Tooltip title={state.reason || 'Unknown state'}>
          <Tag
            color="default"
            icon={<QuestionOutlined />}
            style={{ margin: 0, borderRadius: 6 }}
          >
            N/A
          </Tag>
        </Tooltip>
      </Flex>
    );
  }

  const isEnabled = state.value;

  return (
    <Flex justify="center">
      <Tooltip title={isEnabled ? 'Click to disable' : 'Click to enable'}>
        <Tag
          color={isEnabled ? 'success' : 'error'}
          icon={isEnabled ? <CheckOutlined /> : <CloseOutlined />}
          onClick={onClick}
          style={{
            margin: 0,
            cursor: onClick ? 'pointer' : 'default',
            borderRadius: 6,
            fontWeight: 500,
            transition: 'all 0.2s ease',
          }}
        >
          {isEnabled ? 'ON' : 'OFF'}
        </Tag>
      </Tooltip>
    </Flex>
  );
}

function hasDifferences(row: FeatureMatrixRow, nodeNames: string[]): boolean {
  const values: boolean[] = [];
  for (const nodeName of nodeNames) {
    const cell = row.cells[nodeName];
    if (cell?.kind === 'value') {
      values.push(cell.value);
    }
  }
  if (values.length < 2) return false;
  return values.some((v) => v !== values[0]);
}

export default function FeatureMatrixPage() {
  const [themeMode, toggleTheme] = useTheme();
  const { nodes, rows, errors, isLoadingNodes, isLoading, refresh, toggleFeatureState } = useFeatureMatrix();

  const [searchText, setSearchText] = useState('');
  const [showOnlyDifferences, setShowOnlyDifferences] = useState(false);

  const nodeNames = useMemo(() => nodes.map((n) => n.name), [nodes]);

  const stats = useMemo(() => {
    const applications = new Set(rows.map((r) => r.application));
    let enabledCount = 0;
    let disabledCount = 0;

    rows.forEach((row) => {
      Object.values(row.cells).forEach((cell) => {
        if (cell?.kind === 'value') {
          if (cell.value) enabledCount++;
          else disabledCount++;
        }
      });
    });

    return {
      nodes: nodes.length,
      applications: applications.size,
      features: rows.length,
      enabled: enabledCount,
      disabled: disabledCount,
    };
  }, [nodes, rows]);

  const filteredRows = useMemo(() => {
    let result = rows;

    if (searchText) {
      const lower = searchText.toLowerCase();
      result = result.filter(
        (row) =>
          row.application.toLowerCase().includes(lower) ||
          row.feature.toLowerCase().includes(lower)
      );
    }

    if (showOnlyDifferences) {
      result = result.filter((row) => hasDifferences(row, nodeNames));
    }

    return result;
  }, [rows, searchText, showOnlyDifferences, nodeNames]);

  const columns: ColumnsType<FeatureMatrixRow> = useMemo(() => {
    const nodeMap = new Map<string, NodeDto>(nodes.map((n) => [n.name, n]));

    const baseColumns: ColumnsType<FeatureMatrixRow> = [
      {
        title: (
          <Space>
            <AppstoreOutlined />
            <span>Application</span>
          </Space>
        ),
        dataIndex: 'application',
        key: 'application',
        fixed: 'left',
        width: 200,
        sorter: (a, b) => a.application.localeCompare(b.application),
        render: (text: string) => (
          <Text strong style={{ color: 'inherit' }}>{text}</Text>
        ),
      },
      {
        title: (
          <Space>
            <FlagOutlined />
            <span>Feature</span>
          </Space>
        ),
        dataIndex: 'feature',
        key: 'feature',
        fixed: 'left',
        width: 200,
        sorter: (a, b) => a.feature.localeCompare(b.feature),
        render: (text: string) => (
          <Text code style={{ fontSize: 13 }}>{text}</Text>
        ),
      },
    ];

    const nodeColumns: ColumnsType<FeatureMatrixRow> = nodes.map((node) => ({
      title: (
        <Tooltip title={`Address: ${nodeMap.get(node.name)?.address}`}>
          <Space>
            <CloudServerOutlined />
            <span>{node.name}</span>
          </Space>
        </Tooltip>
      ),
      key: node.name,
      width: 140,
      align: 'center' as const,
      render: (_: unknown, record: FeatureMatrixRow) => {
        const cellState = record.cells[node.name];
        const handleClick = cellState?.kind === 'value'
          ? () => toggleFeatureState(node.id, node.name, record.application, record.feature, cellState.value)
          : undefined;
        return <CellRenderer state={cellState} onClick={handleClick} />;
      },
    }));

    return [...baseColumns, ...nodeColumns];
  }, [nodes, toggleFeatureState]);

  const isDark = themeMode === 'dark';

  const { token } = antTheme.useToken();

  return (
    <ConfigProvider
      theme={{
        algorithm: isDark ? antTheme.darkAlgorithm : antTheme.defaultAlgorithm,
        token: {
          colorPrimary: '#1677ff',
          borderRadius: 6,
        },
      }}
    >
      <Layout style={{ minHeight: '100vh' }}>
        {/* Header */}
        <Header
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 24px',
            background: isDark ? '#141414' : '#fff',
            borderBottom: `1px solid ${isDark ? '#303030' : '#f0f0f0'}`,
            height: 64,
          }}
        >
          <Flex align="center" gap={16}>
            <FlagOutlined style={{ fontSize: 24, color: token.colorPrimary }} />
            <div>
              <Title level={4} style={{ margin: 0, lineHeight: 1.2 }}>
                Feature States
              </Title>
              <Text type="secondary" style={{ fontSize: 12 }}>
                Manage feature flags across environments
              </Text>
            </div>
          </Flex>
          <Space size="middle">
            <Tooltip title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}>
              <Switch
                checked={isDark}
                onChange={toggleTheme}
                checkedChildren={<MoonOutlined />}
                unCheckedChildren={<SunOutlined />}
              />
            </Tooltip>
          </Space>
        </Header>

        <Content
          style={{
            padding: 24,
            background: isDark ? '#000' : '#f5f5f5',
          }}
        >
          {/* Stats Cards */}
          <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
            <Col xs={24} sm={12} lg={6}>
              <Card bordered={false} size="small">
                <Statistic
                  title={<Text type="secondary">Nodes</Text>}
                  value={stats.nodes}
                  prefix={<CloudServerOutlined style={{ color: token.colorPrimary }} />}
                  loading={isLoadingNodes}
                />
              </Card>
            </Col>
            <Col xs={24} sm={12} lg={6}>
              <Card bordered={false} size="small">
                <Statistic
                  title={<Text type="secondary">Applications</Text>}
                  value={stats.applications}
                  prefix={<AppstoreOutlined style={{ color: '#722ed1' }} />}
                  loading={isLoading}
                />
              </Card>
            </Col>
            <Col xs={24} sm={12} lg={6}>
              <Card bordered={false} size="small">
                <Statistic
                  title={<Text type="secondary">Enabled</Text>}
                  value={stats.enabled}
                  prefix={<CheckOutlined style={{ color: '#52c41a' }} />}
                  loading={isLoading}
                  valueStyle={{ color: '#52c41a' }}
                />
              </Card>
            </Col>
            <Col xs={24} sm={12} lg={6}>
              <Card bordered={false} size="small">
                <Statistic
                  title={<Text type="secondary">Disabled</Text>}
                  value={stats.disabled}
                  prefix={<CloseOutlined style={{ color: '#ff4d4f' }} />}
                  loading={isLoading}
                  valueStyle={{ color: '#ff4d4f' }}
                />
              </Card>
            </Col>
          </Row>

          {/* Errors Alert */}
          {errors.length > 0 && (
            <Alert
              type="error"
              showIcon
              closable
              style={{ marginBottom: 16 }}
              message={`${errors.length} error${errors.length !== 1 ? 's' : ''} occurred`}
              description={
                <ul style={{ margin: '8px 0 0 0', paddingLeft: 20 }}>
                  {errors.slice(0, 3).map((err) => (
                    <li key={err.id}>
                      <Text type="secondary">[{err.timestamp.toLocaleTimeString()}]</Text>{' '}
                      <Text code>{err.endpoint}</Text>: {err.message}
                    </li>
                  ))}
                  {errors.length > 3 && (
                    <li><Text type="secondary">...and {errors.length - 3} more</Text></li>
                  )}
                </ul>
              }
            />
          )}

          {/* Main Table Card */}
          <Card
            bordered={false}
            title={
              <Flex justify="space-between" align="center" wrap="wrap" gap={12}>
                <Space size="middle">
                  <Text strong style={{ fontSize: 16 }}>Features</Text>
                  <Tag color="blue" style={{ borderRadius: 10, marginLeft: 4 }}>{filteredRows.length}</Tag>
                </Space>
                <Flex gap={12} wrap="wrap">
                  <Input
                    placeholder="Search applications or features..."
                    prefix={<SearchOutlined style={{ color: token.colorTextPlaceholder }} />}
                    allowClear
                    style={{ width: 280 }}
                    value={searchText}
                    onChange={(e) => setSearchText(e.target.value)}
                  />
                  <Tooltip title="Show only features with different states across nodes">
                    <Button
                      icon={<FilterOutlined />}
                      type={showOnlyDifferences ? 'primary' : 'default'}
                      onClick={() => setShowOnlyDifferences(!showOnlyDifferences)}
                    >
                      Differences
                    </Button>
                  </Tooltip>
                  <Button
                    type="primary"
                    icon={<ReloadOutlined spin={isLoading} />}
                    onClick={refresh}
                    loading={isLoading}
                  >
                    Refresh
                  </Button>
                </Flex>
              </Flex>
            }
          >
            {/* Legend */}
            <Flex gap={24} style={{ marginBottom: 16 }} wrap="wrap">
              <Text type="secondary">Legend:</Text>
              <Space size="large">
                <Space size={4}>
                  <Tag color="success" style={{ borderRadius: 6 }}>ON</Tag>
                  <Text type="secondary">Enabled</Text>
                </Space>
                <Space size={4}>
                  <Tag color="error" style={{ borderRadius: 6 }}>OFF</Tag>
                  <Text type="secondary">Disabled</Text>
                </Space>
                <Space size={4}>
                  <Tag color="default" style={{ borderRadius: 6 }}>N/A</Tag>
                  <Text type="secondary">Not available</Text>
                </Space>
              </Space>
            </Flex>

            <Divider style={{ margin: '0 0 16px 0' }} />

            {/* Table */}
            <Table<FeatureMatrixRow>
              columns={columns}
              dataSource={filteredRows}
              rowKey="key"
              loading={isLoadingNodes}
              size="middle"
              sticky
              scroll={{ x: 'max-content' }}
              pagination={{
                defaultPageSize: 25,
                showSizeChanger: true,
                pageSizeOptions: ['10', '25', '50', '100'],
                showTotal: (total, range) => (
                  <Text type="secondary">
                    Showing {range[0]}-{range[1]} of {total} features
                  </Text>
                ),
                style: { marginBottom: 0 },
              }}
              locale={{
                emptyText: isLoading ? (
                  <Flex vertical align="center" gap={16} style={{ padding: 48 }}>
                    <Spin size="large" />
                    <Text type="secondary">Loading features...</Text>
                  </Flex>
                ) : (
                  <Flex vertical align="center" gap={8} style={{ padding: 48 }}>
                    <FlagOutlined style={{ fontSize: 48, color: token.colorTextDisabled }} />
                    <Text type="secondary">No features found</Text>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      Register applications with features to see them here
                    </Text>
                  </Flex>
                ),
              }}
              rowClassName={(_, index) => index % 2 === 0 ? '' : 'ant-table-row-alt'}
            />
          </Card>
        </Content>
      </Layout>

      <style>{`
        .ant-table-row-alt > td {
          background: ${isDark ? 'rgba(255,255,255,0.02)' : 'rgba(0,0,0,0.015)'} !important;
        }
        .ant-table-row:hover > td {
          background: ${isDark ? 'rgba(22, 119, 255, 0.15)' : 'rgba(22, 119, 255, 0.08)'} !important;
        }
        .ant-table-row:hover > td .ant-typography {
          color: inherit !important;
        }
        .ant-tag {
          transition: transform 0.15s ease, box-shadow 0.15s ease;
        }
        .ant-tag[style*="cursor: pointer"]:hover {
          transform: scale(1.08);
          box-shadow: 0 2px 8px rgba(0,0,0,0.15);
        }
        .ant-table-thead > tr > th {
          background: ${isDark ? '#1f1f1f' : '#fafafa'} !important;
        }
      `}</style>
    </ConfigProvider>
  );
}
