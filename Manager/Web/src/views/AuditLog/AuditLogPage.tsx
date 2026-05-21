import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  ConfigProvider,
  Flex,
  Layout,
  Pagination,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Tooltip,
  Typography,
  theme as antTheme,
} from 'antd';
import {
  ArrowLeftOutlined,
  AuditOutlined,
  FlagOutlined,
  MoonOutlined,
  ReloadOutlined,
  SunOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import { authFetch } from '../../auth';
import { useTheme } from '../FeatureMatrix/theme';
import { useAppVersion } from '../../version/useAppVersion';

const { Header, Content } = Layout;
const { Title, Text } = Typography;

interface AuditEntry {
  id: number;
  username: string;
  action: string;
  details: string | null;
  timestamp: string;
}

const ACTION_COLORS: Record<string, string> = {
  ToggleFeature: 'blue',
  FeaturePermanentlyDeleted: 'red',
  ApplicationPermanentlyDeleted: 'volcano',
};

const PAGE_SIZE = 50;

export default function AuditLogPage() {
  const [themeMode, toggleTheme] = useTheme();
  const navigate = useNavigate();
  const appVersion = useAppVersion();
  const isDark = themeMode === 'dark';

  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [actions, setActions] = useState<string[]>([]);
  const [offset, setOffset] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [actionFilter, setActionFilter] = useState<string | undefined>(undefined);

  const fetchPage = useCallback(async (nextOffset: number, filter: string | undefined) => {
    setIsLoading(true);
    try {
      const url = new URL('/api/audit-log', window.location.origin);
      url.searchParams.set('count', String(PAGE_SIZE));
      url.searchParams.set('offset', String(nextOffset));
      if (filter) url.searchParams.set('action', filter);
      const resp = await authFetch(url.toString().replace(window.location.origin, ''));
      if (!resp.ok) {
        throw new Error(`HTTP ${resp.status}`);
      }
      const data: AuditEntry[] = await resp.json();
      setEntries(data);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const fetchActions = useCallback(async () => {
    const resp = await authFetch('/api/audit-log/actions');
    if (!resp.ok) return;
    const list: string[] = await resp.json();
    setActions(list);
  }, []);

  // Fetch the distinct-actions list once on mount; the dropdown stays stable across pagination.
  useEffect(() => {
    void fetchActions();
  }, [fetchActions]);

  // Refetch whenever offset or filter changes.
  useEffect(() => {
    void fetchPage(offset, actionFilter);
  }, [fetchPage, offset, actionFilter]);

  const handleFilterChange = (value: string | undefined) => {
    setOffset(0); // reset paging when filter changes — old offset is meaningless against new result set
    setActionFilter(value);
  };

  const columns: ColumnsType<AuditEntry> = useMemo(() => [
    {
      title: 'Timestamp',
      dataIndex: 'timestamp',
      key: 'timestamp',
      width: 200,
      render: (v: string) => new Date(v).toLocaleString(),
      sorter: (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'User',
      dataIndex: 'username',
      key: 'username',
      width: 160,
      render: (v: string) => <Text code>{v}</Text>,
    },
    {
      title: 'Action',
      dataIndex: 'action',
      key: 'action',
      width: 220,
      render: (v: string) => <Tag color={ACTION_COLORS[v] ?? 'default'}>{v}</Tag>,
    },
    {
      title: 'Details',
      dataIndex: 'details',
      key: 'details',
      render: (v: string | null) =>
        v ? <Text style={{ whiteSpace: 'pre-wrap' }}>{v}</Text> : <Text type="secondary">—</Text>,
    },
  ], []);

  const { token } = antTheme.useToken();

  return (
    <ConfigProvider
      theme={{
        algorithm: isDark ? antTheme.darkAlgorithm : antTheme.defaultAlgorithm,
        token: { colorPrimary: '#1677ff', borderRadius: 6 },
      }}
    >
      <Layout style={{ minHeight: '100vh' }}>
        <Header
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '16px 24px',
            background: isDark ? '#141414' : '#fff',
            borderBottom: `1px solid ${isDark ? '#303030' : '#f0f0f0'}`,
            height: 'auto',
            lineHeight: 'normal',
          }}
        >
          <Flex align="center" gap={16}>
            <Tooltip title="Back to Feature Matrix">
              <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/')} />
            </Tooltip>
            <FlagOutlined style={{ fontSize: 24, color: token.colorPrimary }} />
            <div>
              <Flex align="baseline" gap={8}>
                <Title level={4} style={{ margin: 0, marginTop: 4, lineHeight: 1.2 }}>
                  <Space><AuditOutlined /> Audit Log</Space>
                </Title>
                {appVersion && <Text type="secondary" style={{ fontSize: 11 }}>v{appVersion}</Text>}
              </Flex>
              <Text type="secondary" style={{ fontSize: 12 }}>
                Admin actions are recorded here
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

        <Content style={{ padding: '24px', background: isDark ? '#000' : '#f5f5f5' }}>
          <Flex justify="space-between" align="center" wrap="wrap" gap={12} style={{ marginBottom: 16 }}>
            <Space>
              <Text type="secondary">Filter:</Text>
              <Select
                allowClear
                placeholder="All actions"
                style={{ width: 260 }}
                value={actionFilter}
                onChange={handleFilterChange}
                options={actions.map((a) => ({ label: a, value: a }))}
              />
            </Space>
            <Space>
              <Button icon={<ReloadOutlined spin={isLoading} />} onClick={() => fetchPage(offset, actionFilter)}>
                Refresh
              </Button>
            </Space>
          </Flex>

          <Table<AuditEntry>
            columns={columns}
            dataSource={entries}
            rowKey="id"
            loading={isLoading}
            size="middle"
            pagination={false}
          />

          <Flex justify="end" style={{ marginTop: 16 }}>
            <Pagination
              current={Math.floor(offset / PAGE_SIZE) + 1}
              pageSize={PAGE_SIZE}
              showSizeChanger={false}
              // The API returns up to PAGE_SIZE per request without a total count, so we drive
              // pagination heuristically: there is a next page only when we got a full page back.
              total={offset + entries.length + (entries.length === PAGE_SIZE ? PAGE_SIZE : 0)}
              onChange={(page) => setOffset((page - 1) * PAGE_SIZE)}
            />
          </Flex>
        </Content>
      </Layout>
    </ConfigProvider>
  );
}
