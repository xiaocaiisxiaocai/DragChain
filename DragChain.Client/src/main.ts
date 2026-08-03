import { createApp } from 'vue';
import { createPinia } from 'pinia';
import ElementPlus from 'element-plus';
import zhCn from 'element-plus/es/locale/lang/zh-cn';
import 'element-plus/dist/index.css';
import './style.css';
import App from './App.vue';
import { router } from './router';
import { handleGlobalError, handleUnhandledRejection } from './utils/errorHandler';

const app = createApp(App);

// 注册全局错误处理器
app.config.errorHandler = handleGlobalError;
window.addEventListener('unhandledrejection', handleUnhandledRejection);

app
  .use(createPinia())
  .use(router)
  .use(ElementPlus, { locale: zhCn })
  .mount('#root');
