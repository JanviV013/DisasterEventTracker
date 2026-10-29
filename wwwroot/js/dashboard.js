// dashboard.js
// Draws the map and the two charts on the dashboard.
// The data comes from the server (window.dashboardData is written by the Razor view).

(function () {
    'use strict';

    var data = window.dashboardData || {};
    var points = data.points || [];
    var detailsBase = data.detailsBase || '/Earthquakes/Details';
    var isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';

    function getColor(mag) {
        if (mag === null || mag === undefined) return '#868e96';
        if (mag >= 5) return '#d73027';
        if (mag >= 4) return '#fc8d59';
        if (mag >= 3) return '#fee08b';
        return '#91cf60';
    }

    // ---------------- Map ----------------
    var mapElement = document.getElementById('map');
    if (mapElement && typeof L !== 'undefined') {
        var map = L.map('map', { minZoom: 2, maxBoundsViscosity: 1.0 }).setView([20, 0], 2);
        map.setMaxBounds(L.latLngBounds(L.latLng(-90, -180), L.latLng(90, 180)));

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors',
            maxZoom: 18
        }).addTo(map);

        points.forEach(function (p) {
            var radius = Math.max(3, (p.mag || 0) * 3);

            var marker = L.circleMarker([p.lat, p.lon], {
                radius: radius,
                fillColor: getColor(p.mag),
                color: '#000',
                weight: 1,
                opacity: 1,
                fillOpacity: 0.7
            }).addTo(map);

            // Build the popup with DOM methods (textContent) so the place name can never inject HTML
            var box = document.createElement('div');

            var title = document.createElement('strong');
            title.textContent = 'Magnitude ' + (p.mag === null || p.mag === undefined ? 'N/A' : p.mag);
            box.appendChild(title);
            box.appendChild(document.createElement('br'));

            var place = document.createElement('span');
            place.textContent = p.place;
            box.appendChild(place);
            box.appendChild(document.createElement('br'));

            var time = document.createElement('span');
            time.textContent = p.time;
            box.appendChild(time);
            box.appendChild(document.createElement('br'));

            var link = document.createElement('a');
            link.href = detailsBase + '/' + encodeURIComponent(p.id);
            link.textContent = 'View details';
            box.appendChild(link);

            marker.bindPopup(box);
        });
    }

    // ---------------- Charts ----------------
    if (typeof Chart === 'undefined') return;

    var textColor = isDark ? '#dee2e6' : '#495057';
    var gridColor = isDark ? 'rgba(255,255,255,0.15)' : 'rgba(0,0,0,0.1)';

    function chartOptions() {
        return {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                x: { ticks: { color: textColor }, grid: { color: gridColor } },
                y: { beginAtZero: true, ticks: { precision: 0, color: textColor }, grid: { color: gridColor } }
            }
        };
    }

    var rangeCanvas = document.getElementById('magnitudeRangeChart');
    if (rangeCanvas) {
        new Chart(rangeCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: ['Below 2', '2 - 3.9', '4 - 4.9', '5 - 5.9', '6+'],
                datasets: [{
                    label: 'Number of earthquakes',
                    data: data.magnitudeBuckets || [0, 0, 0, 0, 0],
                    backgroundColor: ['#91cf60', '#fee08b', '#fc8d59', '#e34a33', '#b30000']
                }]
            },
            options: chartOptions()
        });
    }

    var dayCanvas = document.getElementById('earthquakesPerDayChart');
    if (dayCanvas) {
        new Chart(dayCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: data.dayLabels || [],
                datasets: [{
                    label: 'Earthquakes per day',
                    data: data.dayCounts || [],
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.15)',
                    fill: true,
                    tension: 0.2
                }]
            },
            options: chartOptions()
        });
    }
})();
