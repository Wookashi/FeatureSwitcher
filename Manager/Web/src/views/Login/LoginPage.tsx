import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  ConfigProvider,
  Card,
  Form,
  Input,
  Button,
  Typography,
  Alert,
  Flex,
  Switch,
  Tooltip,
  theme as antTheme,
} from 'antd';
import {
  UserOutlined,
  LockOutlined,
  FlagOutlined,
  SunOutlined,
  MoonOutlined,
} from '@ant-design/icons';
import { setToken, setRole } from '../../auth';
import { useTheme } from '../FeatureMatrix/theme';

const { Title, Text } = Typography;

interface LoginFormValues {
  username: string;
  password: string;
}

export default function LoginPage() {
  const [themeMode, toggleTheme] = useTheme();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isDark = themeMode === 'dark';

  useEffect(() => {
    fetch('/api/auth/setup-required')
      .then((r) => r.json())
      .then((data) => {
        if (data.required) {
          navigate('/setup', { replace: true });
        }
      })
      .catch(() => {
        // ignore - if setup check fails, just show login
      });
  }, [navigate]);

  const onFinish = async (values: LoginFormValues) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        if (response.status === 401) {
          setError('Invalid username or password');
        } else {
          setError(`Login failed (HTTP ${response.status})`);
        }
        return;
      }

      const data = await response.json();
      setToken(data.token);
      setRole(data.role);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Network error');
    } finally {
      setLoading(false);
    }
  };

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
      <Flex
        vertical
        align="center"
        justify="center"
        style={{
          minHeight: '100vh',
          background: isDark ? '#000' : '#f5f5f5',
        }}
      >
        <div style={{ position: 'absolute', top: 16, right: 24 }}>
          <Tooltip title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}>
            <Switch
              checked={isDark}
              onChange={toggleTheme}
              checkedChildren={<MoonOutlined />}
              unCheckedChildren={<SunOutlined />}
            />
          </Tooltip>
        </div>

        <Card
          style={{ width: 380, maxWidth: '90vw' }}
          bordered={false}
        >
          <Flex vertical align="center" gap={8} style={{ marginBottom: 24 }}>
            <FlagOutlined style={{ fontSize: 32, color: '#1677ff' }} />
            <Title level={3} style={{ margin: 0 }}>Feature Switcher</Title>
            <Text type="secondary">Sign in to manage feature flags</Text>
          </Flex>

          {error && (
            <Alert
              type="error"
              message={error}
              showIcon
              closable
              onClose={() => setError(null)}
              style={{ marginBottom: 16 }}
            />
          )}

          <Form<LoginFormValues>
            onFinish={onFinish}
            layout="vertical"
            size="large"
          >
            <Form.Item
              name="username"
              rules={[{ required: true, message: 'Please enter your username' }]}
            >
              <Input
                prefix={<UserOutlined />}
                placeholder="Username"
                autoFocus
              />
            </Form.Item>

            <Form.Item
              name="password"
              rules={[{ required: true, message: 'Please enter your password' }]}
            >
              <Input.Password
                prefix={<LockOutlined />}
                placeholder="Password"
              />
            </Form.Item>

            <Form.Item style={{ marginBottom: 0 }}>
              <Button
                type="primary"
                htmlType="submit"
                loading={loading}
                block
              >
                Sign in
              </Button>
            </Form.Item>
          </Form>
        </Card>
      </Flex>
    </ConfigProvider>
  );
}
