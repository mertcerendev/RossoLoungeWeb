document.addEventListener('DOMContentLoaded', () => {

    // ============================================================
    // 1. GÖZ SİMGESİ İŞLEVLERİ (ÖNCELİKLİ - HATA VERMEZ)
    // ============================================================

    // A. Profil Sayfası İçin (Şifre Göster/Gizle)
    const togglePassword = document.getElementById('toggle-password');
    const passwordField = document.getElementById('password-field');

    if (togglePassword && passwordField) {
        togglePassword.addEventListener('click', function () {
            const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordField.setAttribute('type', type);

            // İkonu değiştir
            this.querySelector('i').classList.toggle('fa-eye');
            this.querySelector('i').classList.toggle('fa-eye-slash');
        });
    }

    // B. Login Sayfası İçin (Şifre Göster/Gizle)
    const togglePasswordLogin = document.getElementById('toggle-password-login');
    const passwordFieldLogin = document.getElementById('password-field-login');

    if (togglePasswordLogin && passwordFieldLogin) {
        togglePasswordLogin.addEventListener('click', function () {
            const type = passwordFieldLogin.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordFieldLogin.setAttribute('type', type);

            this.querySelector('i').classList.toggle('fa-eye');
            this.querySelector('i').classList.toggle('fa-eye-slash');
        });
    }

    // ============================================================
    // 2. NAVİGASYON VE MENU (SADECE ANA SAYFADA ÇALIŞIR)
    // ============================================================
    const hamburger = document.querySelector('.hamburger');
    const navLinks = document.querySelector('.nav-links');
    const allNavLinks = document.querySelectorAll('.nav-links a');

    // Eğer sayfada hamburger menü varsa bu kodları çalıştır
    if (hamburger && navLinks) {

        // Hamburger Tıklama
        hamburger.addEventListener('click', (e) => {
            e.stopPropagation();
            navLinks.classList.toggle('active');
        });

        // Linklere Tıklanınca Kapat
        allNavLinks.forEach(link => {
            link.addEventListener('click', () => {
                if (navLinks.classList.contains('active')) {
                    navLinks.classList.remove('active');
                }
            });
        });

        // Boşluğa Tıklayınca Kapat
        document.addEventListener('click', (e) => {
            if (navLinks.classList.contains('active') && !navLinks.contains(e.target) && !hamburger.contains(e.target)) {
                navLinks.classList.remove('active');
            }
        });

        // Scroll Efektleri
        window.addEventListener('scroll', () => {
            const navbar = document.getElementById('navbar');

            // Navbar Gölge
            if (navbar && !navbar.classList.contains('dark-header')) {
                if (window.scrollY > 50) {
                    navbar.style.boxShadow = '0 2px 5px rgba(0,0,0,0.5)';
                } else {
                    navbar.style.boxShadow = 'none';
                }
            }

            // Kaydırınca Menüyü Kapat
            if (navLinks.classList.contains('active')) {
                navLinks.classList.remove('active');
            }
        });

        // Smooth Scroll (Yumuşak Kaydırma)
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                e.preventDefault();
                const targetID = this.getAttribute('href');

                if (targetID === '#') {
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                    return;
                }

                const targetSection = document.querySelector(targetID);
                if (targetSection) {
                    targetSection.scrollIntoView({ behavior: 'smooth' });
                }
            });
        });
    }

    // ============================================================
    // 3. REZERVASYON VE TAKVİM (SADECE FORM VARSA ÇALIŞIR)
    // ============================================================
    const datePickerElement = document.getElementById('datePicker');
    const form = document.getElementById('reservation-form');

    // Eğer tarih kutusu varsa Flatpickr'ı başlat (Yoksa bu bloğu atla, hata verme)
    if (datePickerElement && form) {

        const fp = flatpickr(datePickerElement, {
            enableTime: true,
            dateFormat: "d.m.Y H:i",
            minDate: "today",
            time_24hr: true,
            locale: "tr",
            disableMobile: "true",
            theme: "dark"
        });

        const phoneInput = document.getElementById('phoneInput');

        // Telefon Input Kontrolü
        if (phoneInput) {
            phoneInput.addEventListener('input', function (e) {
                let cleanVal = this.value.replace(/[^0-9]/g, '');
                if (cleanVal.startsWith('0')) cleanVal = cleanVal.substring(1);
                if (cleanVal.length > 10) cleanVal = cleanVal.slice(0, 10);
                this.value = cleanVal;
            });
        }

        // Form Gönderimi ve Validasyon
        form.addEventListener('submit', (e) => {
            const name = form.querySelector('input[name="AdSoyad"]').value.trim();
            const phone = phoneInput.value.trim();
            const dateVal = document.getElementById('datePicker').value;

            let hasError = false;
            let errorMessage = "";

            // Kural 1: Telefon
            if (phone.length < 10) {
                errorMessage = "Lütfen telefon numaranızı eksiksiz giriniz (Başında 0 olmadan 10 hane).";
                hasError = true;
            }
            // Kural 2: Tarih
            else if (!dateVal) {
                errorMessage = "Lütfen tarih ve saat seçiniz.";
                hasError = true;
            }
            else {
                // Tarih mantık kontrolü
                if (fp.selectedDates.length > 0) {
                    const selectedDateObj = fp.selectedDates[0];
                    const now = new Date();
                    let checkDate = new Date(selectedDateObj);

                    if (checkDate.getHours() === 0 && checkDate.getMinutes() === 0) {
                        checkDate.setDate(checkDate.getDate() + 1);
                    }

                    if (checkDate < now) {
                        errorMessage = "Geçmiş bir saate rezervasyon yapamazsınız.";
                        hasError = true;
                    }
                }
            }

            // Kural 3: İsim
            if (!hasError && name.length < 3) {
                errorMessage = "Lütfen isminizi tam giriniz.";
                hasError = true;
            }

            // HATA VARSA DURDUR
            if (hasError) {
                e.preventDefault();
                alert(errorMessage);
            }
        });
    }
});