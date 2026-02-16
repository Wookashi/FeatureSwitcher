import { useState } from 'react';
import { Modal, Form, Input, message } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { authFetch } from '../../auth';

interface ChangePasswordModalProps {
  open: boolean;
  onClose: () => void;
}

interface PasswordFormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export default function ChangePasswordModal({ open, onClose }: ChangePasswordModalProps) {
  const [form] = Form.useForm<PasswordFormValues>();
  const [loading, setLoading] = useState(false);

  const handleOk = async () => {
    try {
      const values = await form.validateFields();

      if (values.newPassword !== values.confirmPassword) {
        form.setFields([{ name: 'confirmPassword', errors: ['Passwords do not match'] }]);
        return;
      }

      setLoading(true);
      const response = await authFetch('/api/auth/password', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          currentPassword: values.currentPassword,
          newPassword: values.newPassword,
        }),
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        message.error(data?.error ?? 'Failed to change password');
        return;
      }

      message.success('Password changed successfully');
      form.resetFields();
      onClose();
    } catch {
      // validation failed
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      title="Change Password"
      open={open}
      onOk={handleOk}
      onCancel={() => { form.resetFields(); onClose(); }}
      confirmLoading={loading}
      okText="Change Password"
      destroyOnClose
    >
      <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
        <Form.Item
          name="currentPassword"
          label="Current Password"
          rules={[{ required: true, message: 'Please enter your current password' }]}
        >
          <Input.Password prefix={<LockOutlined />} placeholder="Current password" />
        </Form.Item>

        <Form.Item
          name="newPassword"
          label="New Password"
          rules={[{ required: true, message: 'Please enter a new password' }]}
        >
          <Input.Password prefix={<LockOutlined />} placeholder="New password" />
        </Form.Item>

        <Form.Item
          name="confirmPassword"
          label="Confirm New Password"
          rules={[{ required: true, message: 'Please confirm your new password' }]}
        >
          <Input.Password prefix={<LockOutlined />} placeholder="Confirm new password" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
