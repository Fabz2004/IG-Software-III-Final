document.addEventListener("DOMContentLoaded", () => {
    // ====== VALIDACIÓN DE REGISTRO ======
    const registroForm = document.getElementById("registroForm");
    if (registroForm) {
        registroForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const email = document.getElementById("email");
            const password = document.getElementById("password");
            const telefono = document.getElementById("telefono");

            // Limpiar errores visuales previos
            [email, password, telefono].forEach(i => i.classList.remove("border-red-500"));

            // Expresión regular para correo válido
            const emailRegex = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

            // Validación de formato de correo
            if (!emailRegex.test(email.value.trim())) {
                email.classList.add("border-red-500");
                Swal.fire({
                    icon: "error",
                    title: "Correo inválido",
                    text: "Por favor ingresa un correo electronico valido (ejemplo@dominio.com).",
                    confirmButtonColor: "#000"
                });
                return;
            }

            // Validar longitud de contraseña
            if (password.value.trim().length < 8) {
                password.classList.add("border-red-500");
                Swal.fire({
                    icon: "error",
                    title: "Contrasena invalida",
                    text: "Debe tener al menos 8 caracteres.",
                    confirmButtonColor: "#000"
                });
                return;
            }

            // Validar teléfono (solo números y 9 dígitos)
            if (!/^\d{9}$/.test(telefono.value.trim())) {
                telefono.classList.add("border-red-500");
                Swal.fire({
                    icon: "error",
                    title: "Teléfono inválido",
                    text: "Debe tener exactamente 9 digitos numericos.",
                    confirmButtonColor: "#000"
                });
                return;
            }

            // Verificar correo duplicado (AJAX)
            const response = await fetch(`/Cuenta/VerificarCorreo?email=${encodeURIComponent(email.value.trim())}`);
            const existe = await response.json();

            if (existe) {
                email.classList.add("border-red-500");
                Swal.fire({
                    icon: "warning",
                    title: "Correo ya registrado",
                    text: "Por favor utiliza otro correo electronico.",
                    confirmButtonColor: "#000"
                });
                return;
            }

            // ✅ Si todo está bien, enviar el formulario
            registroForm.submit();
        });
    }

    // ====== VALIDACIÓN DE LOGIN ======
    const loginForm = document.getElementById("loginForm");
    if (loginForm) {
        loginForm.addEventListener("submit", (e) => {
            const email = document.getElementById("loginEmail");
            const password = document.getElementById("loginPassword");
            const emailRegex = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

            if (!emailRegex.test(email.value.trim())) {
                e.preventDefault();
                Swal.fire({
                    icon: "error",
                    title: "Correo inválido",
                    text: "Por favor ingresa un correo electronico valido.",
                    confirmButtonColor: "#000"
                });
                email.classList.add("border-red-500");
                return;
            }

            if (password.value.trim() === "") {
                e.preventDefault();
                Swal.fire({
                    icon: "error",
                    title: "Contraseña requerida",
                    text: "Por favor ingresa tu contraseña.",
                    confirmButtonColor: "#000"
                });
                password.classList.add("border-red-500");
                return;
            }
        });
    }
});
