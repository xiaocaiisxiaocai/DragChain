<template>
  <el-config-provider namespace="el">
    <div class="pure-app" :class="{ 'is-collapsed': app.sidebarCollapsed }">
      <aside class="pure-sidebar">
        <div class="pure-brand">
          <span class="brand-mark">DC</span>
          <div class="brand-copy">
            <strong>选型计算</strong>
            <small>DragChain Admin</small>
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
              <el-menu-item index="/trunking/calc">线槽选型</el-menu-item>
              <el-menu-item index="/trunking/catalog">线槽型录</el-menu-item>
            </el-sub-menu>
            <el-sub-menu index="chain">
              <template #title>
                <el-icon><Operation /></el-icon>
                <span>拖链</span>
              </template>
              <el-menu-item index="/chain/calc">拖链选型</el-menu-item>
              <el-menu-item index="/chain/wzl">WZL 型录</el-menu-item>
              <el-menu-item index="/chain/me">ME 型录</el-menu-item>
            </el-sub-menu>
            <el-menu-item index="/pipe-library">
              <el-icon><Collection /></el-icon>
              <template #title>管线库</template>
            </el-menu-item>
            <el-menu-item index="/pipe-modules">
              <el-icon><Collection /></el-icon>
              <template #title>模块库</template>
            </el-menu-item>
          </el-menu>
        </el-scrollbar>
      </aside>

      <main class="pure-main">
        <header class="pure-topbar">
          <button class="icon-action" type="button" @click="app.toggleSidebar">
            <el-icon><Fold v-if="!app.sidebarCollapsed" /><Expand v-else /></el-icon>
          </button>
          <div class="page-heading">
            <span>{{ route.meta.title }}</span>
            <small>内网选型工具</small>
          </div>
        </header>

        <section class="pure-content">
          <router-view />
        </section>
      </main>
    </div>
  </el-config-provider>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router';
import { Collection, Expand, Fold, Grid, Operation } from '@element-plus/icons-vue';
import { useAppStore } from './stores/app';

const app = useAppStore();
const route = useRoute();
</script>
