// wwwroot/js/carousel.js
let currentIndex = 0;
let items = [];
let timer;

function initCarousel() {
    items = document.querySelectorAll('.carousel-item');
    if (items.length > 0) {
        items.forEach((item, index) => {
            item.style.display = index === 0 ? 'block' : 'none';
        });
        startAutoRotation();
    }
}

function startAutoRotation() {
    timer = setInterval(() => {
        currentIndex = (currentIndex + 1) % items.length;
        updateCarousel();
    }, 5000);
}

function updateCarousel() {
    items.forEach((item, index) => {
        item.style.display = index === currentIndex ? 'block' : 'none';
    });
}

function nextItem() {
    clearInterval(timer);
    currentIndex = (currentIndex + 1) % items.length;
    updateCarousel();
    startAutoRotation();
}

function prevItem() {
    clearInterval(timer);
    currentIndex = (currentIndex - 1 + items.length) % items.length;
    updateCarousel();
    startAutoRotation();
}

// Make functions available globally
window.initCarousel = initCarousel;
window.nextItem = nextItem;
window.prevItem = prevItem;