<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { ElButton, ElCheckbox, ElCheckboxGroup, ElDialog, ElForm, ElFormItem, ElInput, ElMessage, ElOption, ElSelect, ElSwitch, ElTable, ElTableColumn, ElTabs, ElTabPane, ElTag } from "element-plus";
import { useRoute, useRouter } from "vue-router";
import AddLine from "~icons/ri/add-line";
import Download2Line from "~icons/ri/download-2-line";
import EditLine from "~icons/ri/edit-line";
import Save3Line from "~icons/ri/save-3-line";
import ShieldCheckLine from "~icons/ri/shield-check-line";
import Upload2Line from "~icons/ri/upload-2-line";
import UserSettingsLine from "~icons/ri/user-settings-line";
import {
  createRbacUser,
  exportRbacUsers,
  getRbacPermissions,
  getRbacRolePermissions,
  getRbacUsers,
  importRbacUsers,
  updateRbacRolePermissions,
  updateRbacUser,
  type RbacPermissionItem,
  type RbacRolePermissions,
  type RbacUser,
  type SaveRbacUser
} from "@/api/user";
import { authVersion, getToken } from "@/utils/auth";

defineOptions({ name: "RbacUsers" });

const users = ref<RbacUser[]>([]);
const permissions = ref<RbacPermissionItem[]>([]);
const rolePermissions = ref<Record<string, string[]>>({});
const route = useRoute();
const router = useRouter();
const activeTab = ref(route.meta.rbacTab === "roles" ? "roles" : "users");
const selectedRole = ref("admin");
const dialogVisible = ref(false);
const dialogTitle = ref("新增用户");
const editingId = ref<number | null>(null);
const importInputRef = ref<HTMLInputElement>();
const importing = ref(false);
const exporting = ref(false);
const form = ref<SaveRbacUser>({
  employeeNo: "",
  name: "",
  password: "",
  role: "user",
  enabled: true
});

const roleLabels: Record<string, string> = {
  super_admin: "超级管理员",
  admin: "管理员",
  editor: "编辑",
  user: "普通用户"
};

const editableRoles = ["admin", "editor", "user"];
const roleOptions = [
  { label: "超级管理员", value: "super_admin" },
  { label: "管理员", value: "admin" },
  { label: "编辑", value: "editor" },
  { label: "普通用户", value: "user" }
];
const isSuperAdmin = computed(() => {
  authVersion.value;
  return getToken()?.roles?.includes("super_admin") ?? false;
});
const selectableRoleOptions = computed(() =>
  isSuperAdmin.value ? roleOptions : roleOptions.filter(item => item.value !== "super_admin")
);
const selectedRolePermissions = computed(() => rolePermissions.value[selectedRole.value] ?? []);
const selectedRoleUserCount = computed(() => users.value.filter(user => user.role === selectedRole.value).length);
const permissionKindLabels: Record<string, string> = {
  menu: "菜单",
  page: "页面",
  api: "接口"
};
const permissionKindGroups = [
  { key: "menu", title: "菜单权限" },
  { key: "page", title: "页面权限" },
  { key: "api", title: "接口权限" }
];
const permissionModules = [
  { key: "sensor", title: "感应器", description: "感应器选型、产品管理和配置项维护" },
  { key: "trunking", title: "线槽", description: "线槽型录和线槽数据维护" },
  { key: "chain", title: "拖链", description: "拖链型录维护" },
  { key: "pipe", title: "管线库", description: "管线、模块和元件库维护" },
  { key: "rbac", title: "系统管理", description: "用户、角色和权限管理" }
];

onMounted(async () => {
  await Promise.all([loadUsers(), loadPermissionMatrix()]);
});

watch(
  () => route.meta.rbacTab,
  tab => {
    activeTab.value = tab === "roles" ? "roles" : "users";
  }
);

async function switchTab(tab: string | number) {
  const target = tab === "roles" ? "/system/roles" : "/system/users";
  if (route.path !== target) await router.push(target);
}

async function loadUsers() {
  users.value = await getRbacUsers();
}

function handlePickImportFile() {
  importInputRef.value?.click();
}

async function handleExportUsers() {
  exporting.value = true;
  try {
    const blob = await exportRbacUsers();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "用户清单.xlsx";
    link.click();
    URL.revokeObjectURL(url);
  } catch (e: any) {
    ElMessage.error(e?.message || "导出失败");
  } finally {
    exporting.value = false;
  }
}

async function handleImportUsers(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = "";
  if (!file) return;

  if (!file.name.toLowerCase().endsWith(".xlsx")) {
    ElMessage.warning("请选择 xlsx 文件");
    return;
  }

  importing.value = true;
  try {
    const data = new FormData();
    data.append("file", file);
    const result = await importRbacUsers(data);
    ElMessage.success(`导入完成：新增 ${result.created} 人，更新 ${result.updated} 人`);
    await Promise.all([loadUsers(), loadPermissionMatrix()]);
  } catch (e: any) {
    const message = e?.message || "";
    try {
      const payload = JSON.parse(message);
      const errors = payload?.errors;
      ElMessage.error(Array.isArray(errors) && errors.length ? errors.slice(0, 3).join("；") : payload?.message || "导入失败");
    } catch {
      ElMessage.error(message || "导入失败");
    }
  } finally {
    importing.value = false;
  }
}

async function loadPermissionMatrix() {
  const [items, roles] = await Promise.all([
    getRbacPermissions(),
    getRbacRolePermissions()
  ]);
  permissions.value = items;
  rolePermissions.value = roles.reduce(
    (map: Record<string, string[]>, item: RbacRolePermissions) => {
      map[item.role] = item.permissions;
      return map;
    },
    {}
  );
  for (const role of editableRoles) {
    rolePermissions.value[role] ??= [];
  }
}

function permissionModule(code: string) {
  if (code.includes(":sensor:") || code.startsWith("api:selector") || code.startsWith("api:products") || code.startsWith("api:taxonomy")) return "sensor";
  if (code.startsWith("api:trunking")) return "trunking";
  if (code.startsWith("api:chain")) return "chain";
  if (code.startsWith("api:pipe")) return "pipe";
  if (code.includes(":trunking:")) return "trunking";
  if (code.includes(":chain:")) return "chain";
  if (code.includes(":pipe:")) return "pipe";
  if (code.includes(":rbac:")) return "rbac";
  return "other";
}

function permissionsByModule(module: string) {
  return permissions.value.filter(item => permissionModule(item.code) === module);
}

function permissionsByModuleAndKind(module: string, kind: string) {
  return permissions.value.filter(item => permissionModule(item.code) === module && item.kind === kind);
}

function permissionMeta(item: RbacPermissionItem) {
  if (item.kind === "api") {
    return `${item.methods.join("/")} ${item.apiPattern ?? ""}`.trim();
  }
  return item.routePath ?? "";
}

function setRole(role: string) {
  selectedRole.value = role;
  rolePermissions.value[role] ??= [];
}

async function saveRole(role: string) {
  if (!isSuperAdmin.value) {
    ElMessage.warning("只有超级管理员可以编辑角色权限");
    return;
  }
  await updateRbacRolePermissions(role, rolePermissions.value[role] ?? []);
  ElMessage.success(`${roleLabels[role]}权限已保存`);
  await loadPermissionMatrix();
}

function openCreate() {
  dialogTitle.value = "新增用户";
  editingId.value = null;
  form.value = { employeeNo: "", name: "", password: "", role: "user", enabled: true };
  dialogVisible.value = true;
}

function openEdit(row: RbacUser) {
  if (!canEditUser(row)) {
    ElMessage.warning("只有超级管理员可以维护超级管理员账号");
    return;
  }
  dialogTitle.value = "编辑用户";
  editingId.value = row.id;
  form.value = {
    employeeNo: row.employeeNo,
    name: row.name,
    password: "",
    role: row.role,
    enabled: row.enabled
  };
  dialogVisible.value = true;
}

function canEditUser(row: RbacUser) {
  return isSuperAdmin.value || row.role !== "super_admin";
}

async function save() {
  if (!form.value.employeeNo || !form.value.name) {
    ElMessage.warning("请填写工号和姓名");
    return;
  }
  if (!editingId.value && !form.value.password) {
    ElMessage.warning("新增用户必须设置密码");
    return;
  }
  if (form.value.password && (
    form.value.password.length < 12
    || !/\p{L}/u.test(form.value.password)
    || !/\p{Nd}/u.test(form.value.password)
    || !/[^\p{L}\p{Nd}]/u.test(form.value.password)
  )) {
    ElMessage.warning("密码至少 12 个字符，并同时包含字母、数字和符号");
    return;
  }
  if (editingId.value) {
    await updateRbacUser(editingId.value, form.value);
    ElMessage.success("用户已更新");
  } else {
    await createRbacUser(form.value);
    ElMessage.success("用户已创建");
  }
  dialogVisible.value = false;
  await loadUsers();
}
</script>

<template>
  <div class="sensor-console sensor-users-page">
    <div class="sensor-toolbar">
      <div class="sensor-toolbar-left">
        <div class="users-summary">
          <UserSettingsLine />
          <span>用户 {{ users.length }} 人 / 角色 {{ editableRoles.length + 1 }} 个 / 权限 {{ permissions.length }} 项</span>
        </div>
      </div>
      <div class="sensor-toolbar-right">
        <template v-if="activeTab === 'users'">
          <input ref="importInputRef" type="file" accept=".xlsx" class="hidden-file-input" @change="handleImportUsers" />
          <el-button :loading="importing" @click="handlePickImportFile"><Upload2Line />导入 xlsx</el-button>
          <el-button :loading="exporting" @click="handleExportUsers"><Download2Line />导出 xlsx</el-button>
          <el-button type="primary" @click="openCreate"><AddLine />新增用户</el-button>
        </template>
      </div>
    </div>

    <el-tabs v-model="activeTab" class="sensor-tabs" @tab-change="switchTab">
      <el-tab-pane label="用户管理" name="users">
        <div class="rbac-section-title">
          <h3>用户角色分配</h3>
          <span>每个用户绑定一个角色，登录后继承该角色的菜单、页面和接口权限。</span>
        </div>
        <div class="sensor-table-wrap">
          <el-table :data="users" border stripe height="100%" style="width: 100%">
            <el-table-column prop="employeeNo" label="工号" width="130" />
            <el-table-column prop="name" label="姓名" min-width="140" />
            <el-table-column label="角色" width="140">
              <template #default="{ row }">
                <el-tag :type="row.role === 'super_admin' ? 'danger' : row.role === 'admin' ? 'warning' : 'info'">
                  {{ roleLabels[row.role] }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="状态" width="100">
              <template #default="{ row }">
                <el-tag :type="row.enabled ? 'success' : 'info'">{{ row.enabled ? "启用" : "停用" }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120">
              <template #default="{ row }">
                <el-button size="small" link type="primary" :disabled="!canEditUser(row)" @click="openEdit(row)"><EditLine />编辑</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-tab-pane>

      <el-tab-pane label="角色权限" name="roles">
        <div class="rbac-section-title">
          <h3>角色权限配置</h3>
          <span>{{ isSuperAdmin ? "按角色分配菜单显示、页面访问和 API 操作权限；超级管理员固定拥有全部权限。" : "当前账号仅可查看角色权限，编辑需使用超级管理员账号。" }}</span>
        </div>
        <div class="role-editor">
          <aside class="role-list">
            <button
              v-for="role in editableRoles"
              :key="role"
              class="role-list-item"
              :class="{ 'is-active': selectedRole === role }"
              type="button"
              @click="setRole(role)"
            >
              <span>{{ roleLabels[role] }}</span>
              <small>{{ users.filter(user => user.role === role).length }} 人 / {{ rolePermissions[role]?.length ?? 0 }} 项权限</small>
            </button>
          </aside>

          <section class="role-editor-main">
            <div class="role-editor-head">
              <div>
                <h3>{{ roleLabels[selectedRole] }}</h3>
                <span>{{ selectedRoleUserCount }} 个用户正在使用，已选择 {{ selectedRolePermissions.length }} 项权限</span>
              </div>
              <el-button v-if="isSuperAdmin" type="primary" @click="saveRole(selectedRole)"><Save3Line />保存权限</el-button>
              <el-tag v-else type="info">只读</el-tag>
            </div>

            <div class="permission-sections">
              <section v-for="module in permissionModules" :key="module.key" class="permission-section">
                <header>
                  <div>
                    <h4><ShieldCheckLine />{{ module.title }}</h4>
                    <span>{{ module.description }}</span>
                  </div>
                  <small>{{ permissionsByModule(module.key).length }} 项</small>
                </header>
                <div class="permission-kind-sections">
                  <div v-for="kindGroup in permissionKindGroups" :key="kindGroup.key" class="permission-kind-section">
                    <div class="permission-kind-title">
                      <span>{{ kindGroup.title }}</span>
                      <small>{{ permissionsByModuleAndKind(module.key, kindGroup.key).length }} 项</small>
                    </div>
                    <el-checkbox-group v-model="rolePermissions[selectedRole]" :disabled="!isSuperAdmin" class="permission-list">
                      <el-checkbox v-for="item in permissionsByModuleAndKind(module.key, kindGroup.key)" :key="item.code" :label="item.code" class="permission-item">
                        <span class="permission-kind">{{ permissionKindLabels[item.kind] }}</span>
                        <span class="permission-name">{{ item.name }}</span>
                        <span class="permission-meta">{{ permissionMeta(item) }}</span>
                      </el-checkbox>
                    </el-checkbox-group>
                  </div>
                </div>
              </section>
            </div>
          </section>
        </div>
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="min(460px, calc(100vw - 24px))">
      <el-form :model="form" label-width="84px" class="dialog-form">
        <el-form-item label="工号" required>
          <el-input v-model="form.employeeNo" placeholder="例如 S0001" />
        </el-form-item>
        <el-form-item label="姓名" required>
          <el-input v-model="form.name" placeholder="例如 张三" />
        </el-form-item>
        <el-form-item label="密码" :required="!editingId">
          <el-input v-model="form.password" type="password" show-password :placeholder="editingId ? '留空则不修改密码' : '至少 12 位，含字母、数字和符号'" />
        </el-form-item>
        <el-form-item label="角色" required>
          <el-select v-model="form.role" style="width: 100%">
            <el-option v-for="item in selectableRoleOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.enabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="save"><Save3Line />保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
@use "../sensor/shared-ui.scss";

.dialog-form :deep(.el-form-item) {
  margin-bottom: 24px;
}

.dialog-form :deep(.el-form-item:last-child) {
  margin-bottom: 0;
}

.sensor-tabs {
  padding: 16px;
  max-width: 100%;
  background: var(--sensor-color-surface);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-md);
  box-shadow: var(--sensor-shadow-sm);
}

.sensor-users-page {
  height: auto;
  min-height: calc(100vh - 129px);
  overflow: visible;
}

.sensor-users-page :deep(.el-tabs),
.sensor-users-page :deep(.el-tabs__content),
.sensor-users-page :deep(.el-tab-pane) {
  display: block;
  min-height: 0;
  overflow: visible;
}

.users-summary {
  display: inline-flex;
  min-height: 34px;
  gap: 8px;
  align-items: center;
  padding: 0 10px;
  font-size: 13px;
  font-weight: 800;
  color: #334155;
  background: var(--sensor-color-bg);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-sm);
}

.users-summary svg {
  width: 16px;
  height: 16px;
  color: var(--sensor-color-primary);
}

.hidden-file-input {
  display: none;
}

.rbac-section-title {
  display: flex;
  gap: 12px;
  align-items: baseline;
  justify-content: space-between;
  margin: 2px 0 12px;
}

.rbac-section-title h3 {
  margin: 0;
  font-size: 15px;
  color: var(--sensor-color-text);
}

.rbac-section-title span {
  font-size: 12px;
  color: #64748b;
}

.role-editor {
  display: grid;
  grid-template-columns: 220px minmax(0, 1fr);
  align-items: stretch;
  min-height: 0;
  overflow: visible;
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-md);
  background: #fff;
}

.role-list {
  display: grid;
  align-content: start;
  gap: 8px;
  padding: 12px;
  background: #f8fafc;
  border-right: 1px solid var(--sensor-color-border);
}

.role-list-item {
  display: grid;
  gap: 6px;
  width: 100%;
  padding: 12px;
  text-align: left;
  cursor: pointer;
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--sensor-radius-sm);
}

.role-list-item:hover {
  background: #fff;
  border-color: #dbeafe;
}

.role-list-item.is-active {
  background: #eff6ff;
  border-color: #93c5fd;
}

.role-list-item span {
  font-size: 14px;
  font-weight: 800;
  color: var(--sensor-color-text);
}

.role-list-item small {
  font-size: 12px;
  color: #64748b;
}

.role-editor-main {
  display: grid;
  grid-template-rows: auto 1fr;
  min-width: 0;
}

.role-editor-head {
  display: flex;
  gap: 16px;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  border-bottom: 1px solid var(--sensor-color-border);
}

.role-editor-head h3 {
  margin: 0 0 4px;
  font-size: 16px;
  color: var(--sensor-color-text);
}

.role-editor-head span {
  font-size: 12px;
  color: #64748b;
}

.permission-sections {
  display: grid;
  gap: 12px;
  align-content: start;
  padding: 14px;
}

.permission-section {
  overflow: visible;
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-sm);
}

.permission-section header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 12px;
  background: #f8fafc;
  border-bottom: 1px solid var(--sensor-color-border);
}

.permission-section h4 {
  display: flex;
  gap: 6px;
  align-items: center;
  margin: 0 0 3px;
  font-size: 13px;
  color: var(--sensor-color-text);
}

.permission-section h4 svg {
  width: 14px;
  height: 14px;
  color: var(--sensor-color-primary);
}

.permission-section header span,
.permission-section header small {
  font-size: 12px;
  color: #64748b;
}

.permission-kind-sections {
  display: grid;
  gap: 10px;
  padding: 10px;
}

.permission-kind-section {
  border: 1px solid #e8eef7;
  border-radius: 6px;
}

.permission-kind-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 7px 9px;
  background: #fbfdff;
  border-bottom: 1px solid #e8eef7;
}

.permission-kind-title span {
  font-size: 12px;
  font-weight: 800;
  color: #334155;
}

.permission-kind-title small {
  font-size: 12px;
  color: #94a3b8;
}

.permission-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(300px, 1fr));
  gap: 0;
  padding: 4px 8px;
}

.permission-item {
  min-height: 38px;
  margin-right: 0;
  padding: 6px 8px;
}

.permission-kind {
  display: inline-flex;
  width: 34px;
  height: 20px;
  align-items: center;
  justify-content: center;
  margin-right: 8px;
  font-size: 12px;
  font-weight: 700;
  color: #2563eb;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 4px;
  vertical-align: middle;
}

.permission-name {
  display: inline-block;
  min-width: 156px;
  font-weight: 600;
}

.permission-meta {
  display: inline-block;
  max-width: min(520px, 100%);
  margin-left: 10px;
  overflow-wrap: anywhere;
  font-size: 12px;
  color: #94a3b8;
  vertical-align: top;
}

@media (max-width: 900px) {
  .sensor-tabs {
    padding: 12px;
  }

  .rbac-section-title {
    display: grid;
    gap: 4px;
  }

  .role-editor {
    grid-template-columns: 1fr;
  }

  .role-list {
    border-right: 0;
    border-bottom: 1px solid var(--sensor-color-border);
  }

  .permission-list {
    grid-template-columns: 1fr;
  }

  .permission-item {
    height: auto;
    align-items: flex-start;
    white-space: normal;
  }

  .permission-list :deep(.el-checkbox__label) {
    min-width: 0;
    line-height: 1.45;
    white-space: normal;
  }
}

@media (max-width: 640px) {
  .sensor-tabs {
    padding: 10px;
  }

  .role-editor-head {
    display: grid;
  }

  .permission-meta {
    display: block;
    margin: 2px 0 0;
  }
}
</style>
