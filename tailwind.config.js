/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Views/**/*.cshtml",
        "./Areas/**/Views/**/*.cshtml",
        "./Pages/**/*.cshtml",
        "./wwwroot/js/**/*.js"
    ],
    theme: { extend: {} },
    plugins: []
}
