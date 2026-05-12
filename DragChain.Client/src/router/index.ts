import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import TrunkingCalcView from '../views/TrunkingCalcView.vue';
import TrunkingCatalogView from '../views/TrunkingCatalogView.vue';
import ChainCalcView from '../views/ChainCalcView.vue';
import WzlCatalogView from '../views/WzlCatalogView.vue';
import MeCatalogView from '../views/MeCatalogView.vue';
import PipeLibraryView from '../views/PipeLibraryView.vue';
import PipeModuleView from '../views/PipeModuleView.vue';
import PipeComponentView from '../views/PipeComponentView.vue';

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/trunking/calc'
  },
  {
    path: '/trunking/calc',
    name: 'trunking-calc',
    component: TrunkingCalcView,
    meta: { title: '线槽选型', group: 'trunking' }
  },
  {
    path: '/trunking/catalog',
    name: 'trunking-catalog',
    component: TrunkingCatalogView,
    meta: { title: '线槽型录', group: 'trunking' }
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
    meta: { title: 'WZL 型录', group: 'chain' }
  },
  {
    path: '/chain/me',
    name: 'me-catalog',
    component: MeCatalogView,
    meta: { title: 'ME 型录', group: 'chain' }
  },
  {
    path: '/pipe-library',
    name: 'pipe-library',
    component: PipeLibraryView,
    meta: { title: '管线库', group: 'pipe' }
  },
  {
    path: '/pipe-modules',
    name: 'pipe-modules',
    component: PipeModuleView,
    meta: { title: '模块库', group: 'pipe' }
  },
  {
    path: '/pipe-components',
    name: 'pipe-components',
    component: PipeComponentView,
    meta: { title: '元件库', group: 'pipe' }
  }
];

export const router = createRouter({
  history: createWebHistory(),
  routes
});
