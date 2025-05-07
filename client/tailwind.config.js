/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./index.html",
        "./src/**/*.{js,ts,jsx,tsx}",
    ],
    theme: {
        extend: {
            colors: {
                "primary": "#8A5A44", // warm brown
                "bg-cream": "#FFF8F0", // light cream
                "bg-white": "#ffffff", // white
                "accent": "#FED49A", // light peach/orange
                "border-brown": "#E5B06E", // light brown
                "secondary": "#FEE5C0", // light peach
            },
        },
    },
    plugins: [],
}