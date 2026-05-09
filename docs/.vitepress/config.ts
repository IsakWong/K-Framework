import { defineConfig } from 'vitepress'

export default defineConfig({
  base: '/K-Framework/',
  title: 'K-Framework',
  description: '轻量级模块化 Unity 游戏框架',
  lang: 'zh-CN',

  themeConfig: {
    nav: [
      { text: '指南', link: '/guide/installation' },
      { text: '模块', link: '/modules/foundation' },
      { text: 'API', link: '/api/services' },
      { text: '变更日志', link: '/changelog' },
    ],

    sidebar: {
      '/guide/': [
        {
          text: '指南',
          items: [
            { text: '安装', link: '/guide/installation' },
            { text: '快速开始', link: '/guide/getting-started' },
            { text: '架构概览', link: '/guide/architecture' },
          ],
        },
      ],
      '/modules/': [
        {
          text: '模块文档',
          items: [
            { text: '基础层', link: '/modules/foundation' },
            { text: '信号系统', link: '/modules/signal' },
            { text: '协程系统', link: '/modules/coroutine' },
            { text: 'UI 系统', link: '/modules/ui' },
            { text: '音频系统', link: '/modules/sound' },
            { text: 'Flow & Trigger', link: '/modules/action' },
            { text: '资源管理', link: '/modules/asset' },
            { text: '事件总线', link: '/modules/eventbus' },
            { text: '状态机', link: '/modules/fsm' },
            { text: '对象池', link: '/modules/pool' },
            { text: 'Unit 系统', link: '/modules/unit' },
          ],
        },
      ],
      '/api/': [
        {
          text: 'API 参考',
          items: [
            { text: '服务接口速查', link: '/api/services' },
          ],
        },
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/IsakWong/K-Framework' },
    ],

    search: {
      provider: 'local',
    },

    outline: {
      level: [2, 3],
    },
  },
})
