/** @type {import('tailwindcss').Config} */
export default {
    content: [
        "./index.html",
        "./src/**/*.{js,ts,jsx,tsx}",
    ],
    theme: {
        extend: {
            colors: {
                background: 'hsl(var(--background) / <alpha-value>)',
                foreground: 'hsl(var(--foreground) / <alpha-value>)',
                primary: {
                    DEFAULT: 'hsl(var(--primary) / <alpha-value>)',
                    foreground: 'hsl(var(--primary-foreground) / <alpha-value>)',
                },
                secondary: {
                    DEFAULT: 'hsl(var(--secondary) / <alpha-value>)',
                    foreground: 'hsl(var(--secondary-foreground) / <alpha-value>)',
                },
                accent: {
                    DEFAULT: 'hsl(var(--accent) / <alpha-value>)',
                    foreground: 'hsl(var(--accent-foreground) / <alpha-value>)',
                },
                'heading-accent': 'hsl(var(--heading-accent) / <alpha-value>)',
                muted: {
                    DEFAULT: 'hsl(var(--muted) / <alpha-value>)',
                    foreground: 'hsl(var(--muted-foreground) / <alpha-value>)',
                },
                card: {
                    DEFAULT: 'hsl(var(--card) / <alpha-value>)',
                    foreground: 'hsl(var(--card-foreground) / <alpha-value>)',
                },
                // Chart colors
                'chart-temperature': 'hsl(var(--chart-temperature) / <alpha-value>)',
                'chart-humidity': 'hsl(var(--chart-humidity) / <alpha-value>)',
                'chart-growth': 'hsl(var(--chart-growth) / <alpha-value>)',
                'chart-4': 'hsl(var(--chart-4) / <alpha-value>)',
                'chart-5': 'hsl(var(--chart-5) / <alpha-value>)',
                // Semantic colors
                success: 'hsl(var(--success) / <alpha-value>)',
                warning: 'hsl(var(--warning) / <alpha-value>)',
                destructive: 'hsl(var(--destructive) / <alpha-value>)',
                neutral: 'hsl(var(--neutral) / <alpha-value>)',
                // Other colors
                border: 'hsl(var(--border) / <alpha-value>)',
                input: 'hsl(var(--input) / <alpha-value>)',
                ring: 'hsl(var(--ring) / <alpha-value>)',
                // Sidebar colors
                sidebar: {
                    background: 'hsl(var(--sidebar-background) / <alpha-value>)',
                    foreground: 'hsl(var(--sidebar-foreground) / <alpha-value>)',
                    border: 'hsl(var(--sidebar-border) / <alpha-value>)',
                    accent: 'hsl(var(--sidebar-accent) / <alpha-value>)',
                    'accent-foreground': 'hsl(var(--sidebar-accent-foreground) / <alpha-value>)',
                    primary: 'hsl(var(--sidebar-primary) / <alpha-value>)',
                    'primary-foreground': 'hsl(var(--sidebar-primary-foreground) / <alpha-value>)',
                },
            },
            borderRadius: {
                DEFAULT: 'var(--radius)',
            },
            spacing: {
                'sidebar': 'var(--sidebar-width)',
                'sidebar-mobile': 'var(--sidebar-width-mobile)',
            },
        },
    },
    plugins: [],
}