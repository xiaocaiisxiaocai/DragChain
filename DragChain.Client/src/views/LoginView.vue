<template>
  <div class="login-page">
    <div class="login-bg-pattern" />
    <div class="login-panel">
      <div class="login-brand">
        <span class="login-brand-mark">SS</span>
        <div class="login-brand-text">
          <strong>选型软件</strong>
          <small>Selection Software</small>
        </div>
      </div>

      <div class="login-body">
        <h2 class="login-title">登录系统</h2>
        <p class="login-subtitle">登录后维护型录、感应器配置和权限</p>

        <el-form label-position="top" @submit.prevent="submit" class="login-form">
          <el-form-item label="工号">
            <el-input
              v-model="form.employeeNo"
              autocomplete="username"
              placeholder="请输入工号"
              size="large"
              :prefix-icon="User"
            />
          </el-form-item>
          <el-form-item label="密码">
            <el-input
              v-model="form.password"
              type="password"
              autocomplete="current-password"
              show-password
              placeholder="请输入密码"
              size="large"
              :prefix-icon="Lock"
            />
          </el-form-item>
          <el-button
            type="primary"
            native-type="submit"
            :loading="loading"
            class="login-button"
            size="large"
          >
            {{ loading ? '登录中...' : '登录' }}
          </el-button>
        </el-form>
      </div>

      <div class="login-footer">
        内部系统 · 仅限授权人员
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElButton, ElForm, ElFormItem, ElInput, ElMessage } from 'element-plus';
import { Lock, User } from '@element-plus/icons-vue';
import { getLogin } from '@/api/user';
import { setToken } from '@/utils/auth';

const route = useRoute();
const router = useRouter();
const loading = ref(false);
const form = reactive({
  employeeNo: '',
  password: ''
});

async function submit() {
  if (!form.employeeNo) {
    ElMessage.warning('请输入工号');
    return;
  }
  if (!form.password) {
    ElMessage.warning('请输入密码');
    return;
  }

  loading.value = true;
  try {
    const res = await getLogin({ employeeNo: form.employeeNo, password: form.password });
    if (!res.success) throw new Error('登录失败');
    setToken(res.data);
    await router.replace(typeof route.query.redirect === 'string' ? route.query.redirect : '/sensor/selector');
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : '登录失败');
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped>
.login-page {
  position: relative;
  display: grid;
  min-height: 100vh;
  place-items: center;
  background: linear-gradient(135deg, #0b1120 0%, #1e293b 50%, #0f172a 100%);
  overflow: hidden;
}

.login-bg-pattern {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(ellipse 80% 60% at 20% 40%, rgba(37, 99, 235, 0.15) 0%, transparent 60%),
    radial-gradient(ellipse 60% 50% at 80% 70%, rgba(59, 130, 246, 0.1) 0%, transparent 50%);
  pointer-events: none;
}

.login-panel {
  position: relative;
  width: min(420px, calc(100vw - 40px));
  background: rgba(255, 255, 255, 0.97);
  border-radius: 16px;
  box-shadow:
    0 20px 60px rgba(0, 0, 0, 0.3),
    0 0 0 1px rgba(255, 255, 255, 0.1);
  overflow: hidden;
}

.login-brand {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 24px 32px;
  background: linear-gradient(135deg, #0b1120 0%, #1e293b 100%);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.login-brand-mark {
  display: inline-flex;
  width: 40px;
  height: 40px;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border-radius: 10px;
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.4), inset 0 1px 0 rgba(255, 255, 255, 0.2);
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.5px;
  font-family: "Fira Code", Consolas, monospace;
  color: #fff;
}

.login-brand-text {
  display: grid;
  gap: 2px;
}

.login-brand-text strong {
  font-size: 15px;
  font-weight: 700;
  color: #fff;
  letter-spacing: 0.3px;
}

.login-brand-text small {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.5);
  font-family: "Fira Code", monospace;
  letter-spacing: 0.3px;
}

.login-body {
  padding: 32px;
}

.login-title {
  margin: 0 0 6px;
  font-size: 22px;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.3px;
}

.login-subtitle {
  margin: 0 0 28px;
  font-size: 13px;
  color: #64748b;
  line-height: 1.5;
}

.login-form :deep(.el-form-item__label) {
  font-size: 13px;
  font-weight: 600;
  color: #334155;
}

.login-form :deep(.el-input__wrapper) {
  border-radius: 8px;
  box-shadow: 0 0 0 1px #e2e8f0;
  transition: box-shadow 0.2s ease, border-color 0.2s ease;
}

.login-form :deep(.el-input__wrapper:hover) {
  box-shadow: 0 0 0 1px #94a3b8;
}

.login-form :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.25);
}

.login-button {
  width: 100%;
  margin-top: 8px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 15px;
  letter-spacing: 0.3px;
  height: 44px;
  background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
  border: none;
  box-shadow: 0 4px 14px rgba(37, 99, 235, 0.3);
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.login-button:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(37, 99, 235, 0.4);
}

.login-button:active {
  transform: translateY(0);
}

.login-footer {
  padding: 14px 32px;
  text-align: center;
  font-size: 11px;
  color: #94a3b8;
  border-top: 1px solid #f1f5f9;
  background: #f8fafc;
  letter-spacing: 0.3px;
}

@media (max-width: 480px) {
  .login-body {
    padding: 24px 20px;
  }

  .login-brand {
    padding: 20px;
  }
}
</style>
