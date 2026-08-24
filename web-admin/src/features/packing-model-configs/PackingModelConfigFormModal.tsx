import { zodResolver } from '@hookform/resolvers/zod';
import { InboxOutlined } from '@ant-design/icons';
import { Alert, AutoComplete, Form, Input, InputNumber, message, Modal, Typography, Upload } from 'antd';
import type { AxiosError } from 'axios';
import type { RcFile } from 'antd/es/upload/interface';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { useCreatePackingModelConfig, useUpdatePackingModelConfig, useUploadPackingTemplate } from './usePackingModelConfigs';
import type { PackingModelConfig } from '../../types/packingModelConfig';

/** Đối chiếu FluentValidation `CreatePackingModelConfigRequestValidator`/`UpdatePackingModelConfigRequestValidator` (backend, AC7). */
const packingModelConfigSchema = z.object({
  model: z.string().min(1, 'Model không được để trống').max(200, 'Model tối đa 200 ký tự'),
  packingQuantity: z.number().int().positive('Quy cách đóng gói phải lớn hơn 0'),
  grossWeight: z.number().positive('Khối lượng phải lớn hơn 0').nullable(),
  partName: z.string().min(1, 'Tên sản phẩm không được để trống').max(200, 'Tên sản phẩm tối đa 200 ký tự'),
  manufacturer: z.string().max(200, 'Nhà sản xuất tối đa 200 ký tự').nullable(),
});

type PackingModelConfigFormValues = z.infer<typeof packingModelConfigSchema>;

const EMPTY_VALUES: PackingModelConfigFormValues = {
  model: '',
  packingQuantity: 0,
  grossWeight: null,
  partName: '',
  manufacturer: '',
};

interface PackingModelConfigFormModalProps {
  open: boolean;
  /** Cấu hình đang sửa — `null` nghĩa là đang tạo mới. */
  editingConfig: PackingModelConfig | null;
  /** Các Model đã có cấu hình — dùng gợi ý autocomplete khi tạo mới (AC9). */
  existingModels: string[];
  onClose: () => void;
}

/** Modal tạo mới/sửa cấu hình Quy cách đóng gói theo Model (US-24) — dùng chung 1 form cho cả 2 chế độ. */
export function PackingModelConfigFormModal({ open, editingConfig, existingModels, onClose }: PackingModelConfigFormModalProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  // Phản ánh NGAY trạng thái "có mẫu tem" sau khi upload thành công trong CÙNG lần mở modal, không chờ
  // `editingConfig` (prop, đến từ query cache của trang cha) refresh lại — invalidate query vẫn chạy song song
  // để lần mở tiếp theo/trang danh sách luôn đúng.
  const [templateStatus, setTemplateStatus] = useState<{ hasTemplate: boolean; updatedByUserName: string | null } | null>(null);
  const createMutation = useCreatePackingModelConfig();
  const updateMutation = useUpdatePackingModelConfig();
  const uploadMutation = useUploadPackingTemplate();
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PackingModelConfigFormValues>({
    resolver: zodResolver(packingModelConfigSchema),
    defaultValues: EMPTY_VALUES,
  });

  useEffect(() => {
    if (open) {
      reset(
        editingConfig
          ? {
              model: editingConfig.model,
              packingQuantity: editingConfig.packingQuantity,
              grossWeight: editingConfig.grossWeight,
              partName: editingConfig.partName,
              manufacturer: editingConfig.manufacturer ?? '',
            }
          : EMPTY_VALUES,
      );
      setErrorMessage(null);
      setTemplateStatus(null);
    }
  }, [open, editingConfig, reset]);

  const onSubmit = async (values: PackingModelConfigFormValues) => {
    setErrorMessage(null);
    try {
      if (editingConfig) {
        await updateMutation.mutateAsync({
          id: editingConfig.id,
          request: {
            packingQuantity: values.packingQuantity,
            grossWeight: values.grossWeight,
            partName: values.partName,
            manufacturer: values.manufacturer || null,
          },
        });
      } else {
        await createMutation.mutateAsync({
          model: values.model,
          packingQuantity: values.packingQuantity,
          grossWeight: values.grossWeight,
          partName: values.partName,
          manufacturer: values.manufacturer || null,
        });
      }
      onClose();
    } catch (error) {
      const axiosError = error as AxiosError<{ title?: string; detail?: string }>;
      setErrorMessage(axiosError.response?.data?.detail ?? axiosError.response?.data?.title ?? 'Lưu cấu hình thất bại, vui lòng thử lại');
    }
  };

  const handleUploadTemplate = async (file: RcFile) => {
    if (!editingConfig) {
      return Upload.LIST_IGNORE;
    }
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      void message.error('Chỉ chấp nhận file mẫu tem định dạng .xlsx');
      return Upload.LIST_IGNORE;
    }
    try {
      const updated = await uploadMutation.mutateAsync({ id: editingConfig.id, file });
      setTemplateStatus({ hasTemplate: updated.hasTemplate, updatedByUserName: updated.templateUpdatedByUserName });
      void message.success('Đã tải lên mẫu tem mới');
    } catch {
      void message.error('Tải lên mẫu tem thất bại, vui lòng thử lại');
    }
    // Không dùng danh sách file hiển thị mặc định của antd — trạng thái "có template" lấy từ editingConfig.hasTemplate
    // (invalidate qua query cache sau khi upload thành công, xem useUploadPackingTemplate).
    return Upload.LIST_IGNORE;
  };

  return (
    <Modal
      title={editingConfig ? `Sửa cấu hình đóng gói — ${editingConfig.model}` : 'Thêm cấu hình đóng gói mới'}
      open={open}
      onOk={handleSubmit(onSubmit)}
      onCancel={onClose}
      confirmLoading={isSubmitting}
      destroyOnHidden
    >
      {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
      <Form layout="vertical">
        <Form.Item label="Model" validateStatus={errors.model ? 'error' : ''} help={errors.model?.message}>
          <Controller
            name="model"
            control={control}
            render={({ field }) =>
              editingConfig ? (
                <Input {...field} disabled />
              ) : (
                <AutoComplete
                  value={field.value}
                  options={existingModels.map((m) => ({ value: m }))}
                  filterOption={(inputValue, option) => (option?.value ?? '').toLowerCase().includes(inputValue.toLowerCase())}
                  onChange={(value) => field.onChange(value)}
                  onBlur={field.onBlur}
                  placeholder="Nhập hoặc chọn Model đã có cấu hình"
                />
              )
            }
          />
        </Form.Item>

        <Form.Item label="Quy cách đóng gói (số lượng sản phẩm/thùng)" validateStatus={errors.packingQuantity ? 'error' : ''} help={errors.packingQuantity?.message}>
          <Controller
            name="packingQuantity"
            control={control}
            render={({ field: { onChange, onBlur, value, name } }) => (
              <InputNumber name={name} value={value} min={1} style={{ width: '100%' }} onChange={(newValue) => onChange(newValue ?? 0)} onBlur={onBlur} />
            )}
          />
        </Form.Item>

        <Form.Item label="Khối lượng (không bắt buộc)" validateStatus={errors.grossWeight ? 'error' : ''} help={errors.grossWeight?.message}>
          <Controller
            name="grossWeight"
            control={control}
            render={({ field: { onChange, onBlur, value, name } }) => (
              <InputNumber
                name={name}
                value={value ?? undefined}
                min={0}
                step={0.1}
                style={{ width: '100%' }}
                onChange={(newValue) => onChange(newValue ?? null)}
                onBlur={onBlur}
              />
            )}
          />
        </Form.Item>

        <Form.Item label="Tên sản phẩm" validateStatus={errors.partName ? 'error' : ''} help={errors.partName?.message}>
          <Controller name="partName" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>

        <Form.Item label="Nhà sản xuất (không bắt buộc)" validateStatus={errors.manufacturer ? 'error' : ''} help={errors.manufacturer?.message}>
          <Controller name="manufacturer" control={control} render={({ field }) => <Input {...field} value={field.value ?? ''} />} />
        </Form.Item>

        <Form.Item label="Mẫu tem in (template .xlsx)">
          {editingConfig ? (
            <>
              <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                {(templateStatus?.hasTemplate ?? editingConfig.hasTemplate)
                  ? `Đã có mẫu tem (cập nhật lần cuối bởi ${(templateStatus?.updatedByUserName ?? editingConfig.templateUpdatedByUserName) ?? '—'})`
                  : 'Chưa có mẫu tem nào được tải lên'}
              </Typography.Text>
              <Upload.Dragger accept=".xlsx" showUploadList={false} beforeUpload={handleUploadTemplate} disabled={uploadMutation.isPending}>
                <p className="ant-upload-drag-icon">
                  <InboxOutlined />
                </p>
                <p className="ant-upload-text">Bấm hoặc kéo thả file .xlsx vào đây để tải lên (thay thế mẫu cũ nếu có)</p>
              </Upload.Dragger>
            </>
          ) : (
            <Typography.Text type="secondary">Lưu cấu hình trước, sau đó mở lại để tải lên mẫu tem.</Typography.Text>
          )}
        </Form.Item>
      </Form>
    </Modal>
  );
}
