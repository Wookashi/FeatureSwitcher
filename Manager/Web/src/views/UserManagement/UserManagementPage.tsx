import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  ConfigProvider,
  Layout,
  Table,
  Tag,
  Button,
  Space,
  Tooltip,
  Typography,
  Modal,
  Form,
  Input,
  Select,
  Popconfirm,
  message,
  Switch,
  Flex,
  theme as antTheme,
} from 'antd';
import {
  UserAddOutlined,
  EditOutlined,
  DeleteOutlined,
  ArrowLeftOutlined,
  SunOutlined,
  MoonOutlined,
  FlagOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import { authFetch, useAuth } from '../../auth';
import { useTheme } from '../FeatureMatrix/theme';

const { Header, Content } = Layout;
const { Title, Text } = Typography;

interface UserRecord {
  id: number;
  username: string;
  role: string;
  createdAt: string;
  updatedAt: string;
  accessibleNodeIds: number[];
}

interface NodeRecord {
  id: number;
  name: string;
  address: string;
}

interface CreateFormValues {
  username: string;
  password: string;
  role: string;
  nodeIds: number[];
}

interface EditFormValues {
  role: string;
  nodeIds: number[];
}

export default function UserManagementPage() {
  const [themeMode, toggleTheme] = useTheme();
  const navigate = useNavigate();
  const { isAdmin } = useAuth();

  const [users, setUsers] = useState<UserRecord[]>([]);
  const [nodes, setNodes] = useState<NodeRecord[]>([]);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserRecord | null>(null);

  const [createForm] = Form.useForm<CreateFormValues>();
  const [editForm] = Form.useForm<EditFormValues>();
  const [createLoading, setCreateLoading] = useState(false);
  const [editLoading, setEditLoading] = useState(false);

  const isDark = themeMode === 'dark';

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [usersRes, nodesRes] = await Promise.all([
        authFetch('/api/users'),
        authFetch('/api/nodes'),
      ]);
      if (usersRes.ok) setUsers(await usersRes.json());
      if (nodesRes.ok) setNodes(await nodesRes.json());
    } catch {
      message.error('Failed to load data');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const handleCreate = async () => {
    try {
      const values = await createForm.validateFields();
      setCreateLoading(true);

      const response = await authFetch('/api/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        message.error(data?.error ?? 'Failed to create user');
        return;
      }

      message.success('User created');
      createForm.resetFields();
      setCreateOpen(false);
      fetchData();
    } catch {
      // validation
    } finally {
      setCreateLoading(false);
    }
  };

  const handleEdit = async () => {
    if (!editingUser) return;
    try {
      const values = await editForm.validateFields();
      setEditLoading(true);

      const response = await authFetch(`/api/users/${editingUser.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        message.error(data?.error ?? 'Failed to update user');
        return;
      }

      message.success('User updated');
      editForm.resetFields();
      setEditOpen(false);
      setEditingUser(null);
      fetchData();
    } catch {
      // validation
    } finally {
      setEditLoading(false);
    }
  };

  const handleDelete = async (id: number) => {
    const response = await authFetch(`/api/users/${id}`, { method: 'DELETE' });
    if (!response.ok) {
      const data = await response.json().catch(() => null);
      message.error(data?.error ?? 'Failed to delete user');
      return;
    }
    message.success('User deleted');
    fetchData();
  };

  const openEdit = (record: UserRecord) => {
    setEditingUser(record);
    editForm.setFieldsValue({
      role: record.role,
      nodeIds: record.accessibleNodeIds,
    });
    setEditOpen(true);
  };

  const nodeMap = new Map(nodes.map((n) => [n.id, n.name]));

  const currentUserId = (() => {
    try {
      const token = localStorage.getItem('jwt_token');
      if (!token) return 0;
      const payload = JSON.parse(atob(token.split('.')[1]));
      const claim = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
      return parseInt(claim, 10) || 0;
    } catch {
      return 0;
    }
  })();

  const roleColor = (role: string) => {
    switch (role) {
      case 'Admin': return 'red';
      case 'Editor': return 'blue';
      case 'Viewer': return 'green';
      default: return 'default';
    }
  };

  const columns: ColumnsType<UserRecord> = [
    {
      title: 'Username',
      dataIndex: 'username',
      key: 'username',
      render: (text: string, record: UserRecord) => (
        <Space>
          <Text strong>{text}</Text>
          {record.id === currentUserId && <Tag color="purple">you</Tag>}
        </Space>
      ),
    },
    {
      title: 'Role',
      dataIndex: 'role',
      key: 'role',
      width: 120,
      render: (role: string) => <Tag color={roleColor(role)}>{role}</Tag>,
    },
    {
      title: 'Accessible Nodes',
      dataIndex: 'accessibleNodeIds',
      key: 'nodes',
      render: (ids: number[], record: UserRecord) => {
        if (record.role === 'Admin') return <Text type="secondary">All nodes</Text>;
        if (ids.length === 0) return <Text type="secondary">None</Text>;
        return (
          <Space wrap>
            {ids.map((id) => (
              <Tag key={id}>{nodeMap.get(id) ?? `Node #${id}`}</Tag>
            ))}
          </Space>
        );
      },
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (val: string) => new Date(val).toLocaleString(),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 120,
      render: (_: unknown, record: UserRecord) => (
        <Space>
          <Tooltip title="Edit">
            <Button
              type="text"
              size="small"
              icon={<EditOutlined />}
              onClick={() => openEdit(record)}
            />
          </Tooltip>
          {record.id !== currentUserId && (
            <Popconfirm
              title="Delete this user?"
              description={`Are you sure you want to delete "${record.username}"?`}
              onConfirm={() => handleDelete(record.id)}
              okText="Delete"
              okType="danger"
            >
              <Tooltip title="Delete">
                <Button type="text" size="small" danger icon={<DeleteOutlined />} />
              </Tooltip>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  if (!isAdmin) {
    return null;
  }

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
            <Button
              type="text"
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate('/')}
            />
            <TeamOutlined style={{ fontSize: 24, color: token.colorPrimary }} />
            <div>
              <Title level={4} style={{ margin: 0, marginTop: 4, lineHeight: 1.2 }}>
                User Management
              </Title>
              <Text type="secondary" style={{ fontSize: 12 }}>
                Manage users and their access
              </Text>
            </div>
          </Flex>
          <Space size="middle">
            <Button
              type="primary"
              icon={<UserAddOutlined />}
              onClick={() => { createForm.resetFields(); setCreateOpen(true); }}
            >
              Add User
            </Button>
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
            padding: '32px 24px 24px 24px',
            background: isDark ? '#000' : '#f5f5f5',
          }}
        >
          <Table<UserRecord>
            columns={columns}
            dataSource={users}
            rowKey="id"
            loading={loading}
            pagination={false}
          />
        </Content>
      </Layout>

      {/* Create User Modal */}
      <Modal
        title="Add User"
        open={createOpen}
        onOk={handleCreate}
        onCancel={() => { createForm.resetFields(); setCreateOpen(false); }}
        confirmLoading={createLoading}
        okText="Create"
        destroyOnClose
      >
        <Form form={createForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="username"
            label="Username"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Input placeholder="Username" />
          </Form.Item>
          <Form.Item
            name="password"
            label="Password"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Input.Password placeholder="Password" />
          </Form.Item>
          <Form.Item
            name="role"
            label="Role"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Select placeholder="Select role">
              <Select.Option value="Admin">Admin</Select.Option>
              <Select.Option value="Editor">Editor</Select.Option>
              <Select.Option value="Viewer">Viewer</Select.Option>
            </Select>
          </Form.Item>
          <Form.Item name="nodeIds" label="Accessible Nodes">
            <Select mode="multiple" placeholder="Select nodes (Admins have access to all)">
              {nodes.map((n) => (
                <Select.Option key={n.id} value={n.id}>{n.name}</Select.Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>

      {/* Edit User Modal */}
      <Modal
        title={`Edit User: ${editingUser?.username ?? ''}`}
        open={editOpen}
        onOk={handleEdit}
        onCancel={() => { editForm.resetFields(); setEditOpen(false); setEditingUser(null); }}
        confirmLoading={editLoading}
        okText="Save"
        destroyOnClose
      >
        <Form form={editForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="role"
            label="Role"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Select>
              <Select.Option value="Admin">Admin</Select.Option>
              <Select.Option value="Editor">Editor</Select.Option>
              <Select.Option value="Viewer">Viewer</Select.Option>
            </Select>
          </Form.Item>
          <Form.Item name="nodeIds" label="Accessible Nodes">
            <Select mode="multiple" placeholder="Select nodes (Admins have access to all)">
              {nodes.map((n) => (
                <Select.Option key={n.id} value={n.id}>{n.name}</Select.Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </ConfigProvider>
  );
}
