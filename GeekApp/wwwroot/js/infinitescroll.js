// scrollHelpers.js
function getScrollPosition() {
    return window.scrollY;
}

function setScrollPosition(position) {
    window.scrollTo(0, position);
}

function setupInfiniteScroll(dotNetHelper) {
    let isScrolling = false;
    let scrollTimeout;

    window.addEventListener('scroll', function () {
        if (isScrolling) return;

        isScrolling = true;
        clearTimeout(scrollTimeout);

        scrollTimeout = setTimeout(() => {
            // Check if we're near bottom (within 500px)
            if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 500) {
                dotNetHelper.invokeMethodAsync('LoadMoreIfAtBottom');
            }

            isScrolling = false;
        }, 100);
    }, { passive: true });
}

// Make functions available globally
window.scrollHelpers = {
    getScrollPosition,
    setScrollPosition,
    setupInfiniteScroll
};