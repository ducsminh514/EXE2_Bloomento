/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.razor",
    "./**/*.html",
    "./**/*.cshtml",
    "./Pages/**/*.{razor,html}",
    "./Shared/**/*.{razor,html}",
    "./Features/**/*.{razor,html}",
    "./wwwroot/index.html"
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}