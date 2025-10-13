module.exports = {
  darkMode: 'media',
  content: ['./src/**/*.{html,js,svelte,ts}'],
  theme: {
    extend: {
      colors: {
        // Dark Quartz palette (replaces previous)
        background: '#0b0f14',
        surface: '#151b23',
        surfaceAlt: '#1d252f',
        border: '#27313c',
        primary: '#c877ff', // mapped to accent
        primaryAccent: '#a956f5', // stronger accent
        primaryMuted: '#8a7bb5',
        accent: '#c877ff',
        accentStrong: '#a956f5',
        accentMuted: '#8a7bb5',
        textPrimary: '#f3f5f8',
        textSecondary: '#9aa5b4',
        focusRing: '#c877ff',
        tableRowHover: '#1f2732',
        danger: '#ff5f56',
        warning: '#f0b429',
        success: '#44d27d'
      }
    }
  },
  plugins: []
};
