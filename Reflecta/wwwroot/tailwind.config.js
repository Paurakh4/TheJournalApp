/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "../Components/**/*.{razor,html,cshtml}",
    "./index.html"
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Material You Theme - Uses CSS Variables for Light/Dark mode switching
        surface: {
          bg: 'var(--surface-bg)',
          elevated: 'var(--surface-elevated)',
          'elevated-hover': 'color-mix(in srgb, var(--surface-elevated) 85%, var(--text-primary) 15%)',
          'elevated-active': 'color-mix(in srgb, var(--surface-elevated) 75%, var(--text-primary) 25%)',
          container: 'var(--surface-container)',
        },
        accent: {
          primary: 'var(--accent-primary)',
          'primary-hover': 'var(--accent-primary-hover)',
          'primary-active': 'var(--accent-primary-active)',
          'primary-20': 'var(--accent-primary-20)',
          'primary-30': 'var(--accent-primary-30)',
          'primary-50': 'var(--accent-primary-50)',
          secondary: 'var(--accent-secondary)',
          tertiary: 'var(--accent-tertiary)',
        },
        text: {
          primary: 'var(--text-primary)',
          secondary: 'var(--text-secondary)',
          muted: 'var(--text-muted)',
          inverse: 'var(--text-inverse)',
        },
        mood: {
          positive: 'var(--mood-positive)',
          'positive-20': 'var(--mood-positive-20)',
          'positive-30': 'var(--mood-positive-30)',
          neutral: 'var(--mood-neutral)',
          'neutral-20': 'var(--mood-neutral-20)',
          'neutral-30': 'var(--mood-neutral-30)',
          negative: 'var(--mood-negative)',
          'negative-20': 'var(--mood-negative-20)',
          'negative-30': 'var(--mood-negative-30)',
          calm: 'var(--mood-calm)',
          'calm-20': 'var(--mood-calm-20)',
          'calm-30': 'var(--mood-calm-30)',
          energetic: 'var(--mood-energetic)',
          'energetic-20': 'var(--mood-energetic-20)',
          'energetic-30': 'var(--mood-energetic-30)',
        },
        border: {
          subtle: 'var(--border-subtle)',
          DEFAULT: 'var(--border-default)',
        }
      },
      fontFamily: {
        sans: ['Outfit', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        'xl': '1rem',
        '2xl': '1.5rem',
        '3xl': '2rem',
        '4xl': '2.5rem',
        'pill': '9999px',
      },
      spacing: {
        'rail': '80px',
        'rail-expanded': '240px',
        'safe-top': 'env(safe-area-inset-top)',
        'safe-bottom': 'env(safe-area-inset-bottom)',
      },
      animation: {
        'fade-in': 'fadeIn 0.3s ease-out',
        'slide-up': 'slideUp 0.4s ease-out',
        'slide-in-right': 'slideInRight 0.3s ease-out',
        'scale-in': 'scaleIn 0.2s ease-out',
        'pulse-soft': 'pulseSoft 2s ease-in-out infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideInRight: {
          '0%': { opacity: '0', transform: 'translateX(-20px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        scaleIn: {
          '0%': { opacity: '0', transform: 'scale(0.95)' },
          '100%': { opacity: '1', transform: 'scale(1)' },
        },
        pulseSoft: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.7' },
        },
      },
      boxShadow: {
        'elevated': '0 4px 24px var(--shadow-color)',
        'elevated-lg': '0 8px 40px var(--shadow-color)',
        'glow-accent': '0 0 20px color-mix(in srgb, var(--accent-primary) 30%, transparent)',
      }
    },
  },
  plugins: [require('@tailwindcss/typography')],
}
