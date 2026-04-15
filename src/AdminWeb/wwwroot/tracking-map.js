window.trackingMap = {
    _maps: {},
    _markers: {},

    initMap: function (mapId, lat, lng, zoom) {
        var existing = this._maps[mapId];
        if (existing) {
            existing.remove();
            delete this._maps[mapId];
            delete this._markers[mapId];
        }
        var map = L.map(mapId).setView([lat, lng], zoom || 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(map);
        this._maps[mapId] = map;
        this._markers[mapId] = {};
    },

    setMarker: function (mapId, markerId, lat, lng, label) {
        var map = this._maps[mapId];
        if (!map) return;
        if (!this._markers[mapId]) this._markers[mapId] = {};
        var existing = this._markers[mapId][markerId];
        if (existing) {
            existing.setLatLng([lat, lng]);
            if (label) existing.setPopupContent(label);
        } else {
            var marker = L.marker([lat, lng]).addTo(map);
            if (label) marker.bindPopup(label);
            this._markers[mapId][markerId] = marker;
        }
    },

    panTo: function (mapId, lat, lng) {
        var map = this._maps[mapId];
        if (map) map.panTo([lat, lng]);
    },

    removeMarker: function (mapId, markerId) {
        var map = this._maps[mapId];
        if (!map || !this._markers[mapId]) return;
        var marker = this._markers[mapId][markerId];
        if (marker) {
            map.removeLayer(marker);
            delete this._markers[mapId][markerId];
        }
    },

    removeMap: function (mapId) {
        var map = this._maps[mapId];
        if (map) {
            map.remove();
            delete this._maps[mapId];
            delete this._markers[mapId];
        }
    }
};
