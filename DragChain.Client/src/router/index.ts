import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import TrunkingCalcView from '../views/TrunkingCalcView.vue';
import TrunkingCatalogView from '../views/TrunkingCatalogView.vue';
import TrunkingSavedSelectionsView from '../views/TrunkingSavedSelectionsView.vue';
import ChainCalcView from '../views/ChainCalcView.vue';
import WzlCatalogView from '../views/WzlCatalogView.vue';
import MeCatalogView from '../views/MeCatalogView.vue';
import PipeLibraryView from '../views/PipeLibraryView.vue';
import PipeModuleView from '../views/PipeModuleView.vue';
import PipeComponentView from '../views/PipeComponentView.vue';
import LoginView from '../views/LoginView.vue';
import SensorSelectorView from '../views/sensor/SelectorView.vue';
import SensorProductsView from '../views/sensor/ProductsView.vue';
import RbacUsersView from '../views/rbac/UsersView.vue';
import { hasPerms, isLoggedIn } from '../utils/auth';

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/trunking/calc'
  },
  {
    path: '/login',
    name: 'login',
    component: LoginView,
    meta: { title: '登录', publicAccess: true }
  },
  {
    path: '/trunking/calc',
    name: 'trunking-calc',
    component: TrunkingCalcView,
    meta: { title: '线槽选型', group: 'trunking' }
  },
  {
    path: '/trunking/saved',
    name: 'trunking-saved',
    component: TrunkingSavedSelectionsView,
    meta: { title: '保存选型', group: 'trunking' }
  },
  {
    path: '/trunking/catalog',
    name: 'trunking-catalog',
    component: TrunkingCatalogView,
    meta: { title: '线槽型录', group: 'trunking', pagePerm: 'page:trunking:catalog' }
  },
  {
    path: '/chain/calc',
    name: 'chain-calc',
    component: ChainCalcView,
    meta: { title: '拖链选型', group: 'chain' }
  },
  {
    path: '/chain/wzl',
    name: 'wzl-catalog',
    component: WzlCatalogView,
    meta: { title: 'WZL 型录', group: 'chain', pagePerm: 'page:chain:catalog' }
  },
  {
    path: '/chain/me',
    name: 'me-catalog',
    component: MeCatalogView,
    meta: { title: 'ME 型录', group: 'chain', pagePerm: 'page:chain:catalog' }
  },
  {
    path: '/pipe-library',
    name: 'pipe-library',
    component: PipeLibraryView,
    meta: { title: '管线库', group: 'pipe', pagePerm: 'page:pipe:library' }
  },
  {
    path: '/pipe-modules',
    name: 'pipe-modules',
    component: PipeModuleView,
    meta: { title: '模块库', group: 'pipe', pagePerm: 'page:pipe:library' }
  },
  {
    path: '/pipe-components',
    name: 'pipe-components',
    component: PipeComponentView,
    meta: { title: '元件库', group: 'pipe', pagePerm: 'page:pipe:library' }
  },
  {
    path: '/sensor/selector',
    name: 'sensor-selector',
    component: SensorSelectorView,
    meta: { title: '感应器选型', group: 'sensor', pagePerm: 'page:sensor:selector' }
  },
  {
    path: '/sensor/products',
    name: 'sensor-products',
    component: SensorProductsView,
    meta: { title: '产品管理', group: 'sensor', pagePerm: 'page:sensor:products' }
  },
  {
    path: '/rbac/users',
    redirect: '/system/users'
  },
  {
    path: '/system/users',
    name: 'system-users',
    component: RbacUsersView,
    meta: { title: '用户管理', group: 'system', pagePerm: 'page:rbac:users', rbacTab: 'users', scrollContent: true }
  },
  {
    path: '/system/roles',
    name: 'system-roles',
    component: RbacUsersView,
    meta: { title: '角色权限', group: 'system', pagePerm: 'page:rbac:roles', rbacTab: 'roles', scrollContent: true }
  }
];

export const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach(to => {
  document.title = `${String(to.meta.title ?? '选型软件')} | 选型软件`;
  if (to.meta.publicAccess) return true;

  if (!isLoggedIn()) {
    return { path: '/login', query: { redirect: to.fullPath } };
  }

  const pagePerm = to.meta.pagePerm as string | undefined;
  if (pagePerm && !hasPerms(pagePerm)) {
    return '/trunking/calc';
  }

  return true;
});
