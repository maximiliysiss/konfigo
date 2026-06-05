/** @type {import('tailwindcss').Config} */
module.exports = {
	content: ['./src/**/*.{html,js,svelte,ts}'],
	theme: {
		extend: {
			colors: {
				bg: 'rgb(var(--color-bg) / <alpha-value>)',
				surface: 'rgb(var(--color-surface) / <alpha-value>)',
				muted: 'rgb(var(--color-muted) / <alpha-value>)',
				text: 'rgb(var(--color-text) / <alpha-value>)',
				accent: 'rgb(var(--color-accent) / <alpha-value>)'
			}
		}
	},
	plugins: []
};
