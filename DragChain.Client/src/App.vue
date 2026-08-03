<template>
  <el-config-provider namespace="el">
    <!-- 全局 Loading -->
    <GlobalLoading />

    <!-- 离线提示 -->
    <OfflineBanner />

    <!-- Login page: full-screen, no shell -->
    <router-view v-if="isLoginPage" />

    <!-- Normal pages: app shell -->
    <div v-else class="pure-app" :class="{ 'is-collapsed': app.sidebarCollapsed }">
      <aside class="pure-sidebar">
        <div class="pure-brand">
          <span class="brand-mark">SS</span>
          <div class="brand-copy">
            <strong>选型软件</strong>
            <small>Selection Software</small>
          </div>
        </div>

        <el-scrollbar class="menu-scroll">
          <el-menu
            router
            :default-active="route.path"
            :collapse="app.sidebarCollapsed"
            class="pure-menu"
          >
            <el-sub-menu index="trunking">
              <template #title>
                <el-icon><Grid /></el-icon>
                <span>线槽</span>
              </template>
              <el-menu-item index="/trunking/calc">
                <template #title>
                  <span class="menu-item-dot" />
                  线槽选型
                </template>
              </el-menu-item>
              <el-menu-item index="/trunking/saved">
                <template #title>
                  <span class="menu-item-dot" />
                  保存选型
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:trunking:catalog')" index="/trunking/catalog">
                <template #title>
                  <span class="menu-item-dot" />
                  线槽型录
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:pipe:library')" index="/pipe-library">
                <template #title>
                  <span class="menu-item-dot" />
                  管线库
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:pipe:library')" index="/pipe-modules">
                <template #title>
                  <span class="menu-item-dot" />
                  模块库
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:pipe:library')" index="/pipe-components">
                <template #title>
                  <span class="menu-item-dot" />
                  元件库
                </template>
              </el-menu-item>
            </el-sub-menu>

            <el-sub-menu index="chain">
              <template #title>
                <el-icon><Operation /></el-icon>
                <span>拖链</span>
              </template>
              <el-menu-item index="/chain/calc">
                <template #title>
                  <span class="menu-item-dot" />
                  拖链选型
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:chain:catalog')" index="/chain/wzl">
                <template #title>
                  <span class="menu-item-dot" />
                  WZL 型录
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:chain:catalog')" index="/chain/me">
                <template #title>
                  <span class="menu-item-dot" />
                  ME 型录
                </template>
              </el-menu-item>
            </el-sub-menu>

            <el-sub-menu index="sensor">
              <template #title>
                <el-icon><Cpu /></el-icon>
                <span>感应器</span>
              </template>
              <el-menu-item v-if="can('page:sensor:selector')" index="/sensor/selector">
                <template #title>
                  <span class="menu-item-dot" />
                  感应器选型
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:sensor:products')" index="/sensor/products">
                <template #title>
                  <span class="menu-item-dot" />
                  产品管理
                </template>
              </el-menu-item>
            </el-sub-menu>

            <el-sub-menu v-if="can('menu:rbac')" index="system">
              <template #title>
                <el-icon><User /></el-icon>
                <span>系统管理</span>
              </template>
              <el-menu-item v-if="can('page:rbac:users')" index="/system/users">
                <template #title>
                  <span class="menu-item-dot" />
                  用户管理
                </template>
              </el-menu-item>
              <el-menu-item v-if="can('page:rbac:roles')" index="/system/roles">
                <template #title>
                  <span class="menu-item-dot" />
                  角色权限
                </template>
              </el-menu-item>
            </el-sub-menu>
          </el-menu>
        </el-scrollbar>
      </aside>

      <main class="pure-main">
        <header class="pure-topbar">
          <button class="icon-action" type="button" :title="app.sidebarCollapsed ? '展开侧边栏' : '折叠侧边栏'" @click="app.toggleSidebar">
            <el-icon><Fold v-if="!app.sidebarCollapsed" /><Expand v-else /></el-icon>
          </button>
          <div class="page-heading">
            <span>{{ route.meta.title }}</span>
            <small>内网选型工具</small>
          </div>

          <div class="topbar-spacer" />

          <div class="topbar-right">
            <span class="topbar-badge">
              <span class="topbar-badge-dot" />
              系统正常
            </span>
            <button v-if="!loggedIn" class="topbar-login-btn" @click="goLogin">
              <el-icon><User /></el-icon>
              登录
            </button>
            <button v-else class="topbar-logout-btn" @click="logout">
              <el-icon><SwitchButton /></el-icon>
              退出
            </button>
          </div>
        </header>

        <section class="pure-content" :class="{ 'pure-content-scroll': route.meta.scrollContent }">
          <router-view v-slot="{ Component }">
            <keep-alive>
              <component :is="Component" />
            </keep-alive>
          </router-view>
        </section>
      </main>
    </div>
  </el-config-provider>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useRouter } from 'vue-router';
import { Cpu, Expand, Fold, Grid, Operation, SwitchButton, User } from '@element-plus/icons-vue';
import { useAppStore } from './stores/app';
import { authVersion, hasPerms, isLoggedIn, removeToken } from './utils/auth';
import GlobalLoading from './components/GlobalLoading.vue';
import OfflineBanner from './components/OfflineBanner.vue';

const app = useAppStore();
const route = useRoute();
const router = useRouter();
const isLoginPage = computed(() => route.path === '/login');
const loggedIn = computed(() => { void authVersion.value; return isLoggedIn(); });

function can(permission: string) {
  void authVersion.value;
  return hasPerms(permission);
}

async function logout() {
  removeToken();
  await router.replace('/login');
}

async function goLogin() {
  await router.push({ path: '/login', query: { redirect: route.fullPath } });
}
</script>

<style>
/* Menu section divider */
.menu-section-divider {
  margin: 6px 14px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

/* Sub-menu item dot indicator */
.menu-item-dot {
  display: inline-block;
  width: 4px;
  height: 4px;
  border-radius: 50%;
  background: currentColor;
  opacity: 0.5;
  margin-right: 4px;
  vertical-align: middle;
  flex-shrink: 0;
}

.el-menu-item.is-active .menu-item-dot {
  opacity: 1;
  background: #fff;
  box-shadow: 0 0 4px rgba(255, 255, 255, 0.6);
}

/* ---- Topbar Login/Logout Buttons ---- */
.topbar-login-btn,
.topbar-logout-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s ease;
}

.topbar-login-btn {
  background: #2563eb;
  color: #fff;
  box-shadow: 0 1px 3px rgba(37, 99, 235, 0.3);
}

.topbar-login-btn:hover {
  background: #1d4ed8;
  box-shadow: 0 2px 8px rgba(37, 99, 235, 0.4);
  transform: translateY(-1px);
}

.topbar-login-btn:active {
  transform: translateY(0);
  box-shadow: 0 1px 2px rgba(37, 99, 235, 0.3);
}

.topbar-logout-btn {
  background: #fff;
  color: #64748b;
  border-color: #e2e8f0;
}

.topbar-logout-btn:hover {
  color: #dc2626;
  border-color: #fca5a5;
  background: #fef2f2;
}

.topbar-logout-btn:active {
  background: #fee2e2;
}
</style>
