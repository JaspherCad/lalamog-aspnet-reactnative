import { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { MapLocation } from './types';

export interface VenueMarker { id: number; latitude: number; longitude: number; label: string; isPublic?: boolean }
const emptyMarkers: VenueMarker[] = [];
const pinIcon = L.divIcon({ className: 'venue-marker', html: '<span aria-hidden="true">●</span>', iconSize: [24, 24], iconAnchor: [12, 12] });
const gymIcon = L.divIcon({ className: 'venue-marker public-gym-marker', html: '<span aria-hidden="true">◆</span>', iconSize: [32, 32], iconAnchor: [16, 16] });

export function LocationMap({ value, onChange, markers = emptyMarkers, onSelect, fullscreen = false }: {
  value: MapLocation; onChange?: (value: MapLocation) => void; markers?: VenueMarker[]; onSelect?: (id: number) => void; fullscreen?: boolean;
}) {
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const marker = useRef<L.Marker | null>(null);
  const latest = useRef({ value, onChange, onSelect });
  latest.current = { value, onChange, onSelect };
  const [tileError, setTileError] = useState(false);
  useEffect(() => {
    if (!container.current) return;
    const view = L.map(container.current, { scrollWheelZoom: false }).setView([14.6, 120.98], 10);
    map.current = view;
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      maxZoom: 19, noWrap: true
    }).on('tileerror', () => setTileError(true)).addTo(view);
    if (fullscreen) {
      view.zoomControl.setPosition('bottomright');
      view.attributionControl.setPosition('bottomleft');
    }
    const observer = new ResizeObserver(() => view.invalidateSize({ pan: false }));
    observer.observe(container.current);
    view.on('click', (event: L.LeafletMouseEvent) => {
      if (!latest.current.onChange) return;
      const point = event.latlng.wrap();
      latest.current.onChange({ ...latest.current.value, latitude: point.lat, longitude: point.lng });
    });
    return () => { observer.disconnect(); view.remove(); map.current = null; marker.current = null; };
  }, [fullscreen]);
  useEffect(() => {
    const view = map.current;
    if (!view) return;
    const group = L.layerGroup().addTo(view);
    for (const point of markers) {
      const label = document.createElement('span'); label.textContent = point.label;
      L.marker([point.latitude, point.longitude], { icon: point.isPublic ? gymIcon : pinIcon, title: point.label, alt: point.label }).bindTooltip(label)
        .on('click', () => latest.current.onSelect?.(point.id))
        .on('keypress', (event: L.LeafletEvent) => {
          const key = (event as L.LeafletKeyboardEvent).originalEvent;
          if (key.key === 'Enter' || key.key === ' ') { key.preventDefault(); latest.current.onSelect?.(point.id); }
        }).addTo(group);
    }
    if (markers.length) view.fitBounds(L.latLngBounds(markers.map(point => [point.latitude, point.longitude])), {
      paddingTopLeft: fullscreen ? [48, Math.min(140, view.getSize().y * .28)] : [30, 30],
      paddingBottomRight: fullscreen ? [48, Math.min(140, view.getSize().y * .28)] : [30, 30], maxZoom: 17, animate: false
    });
    return () => { group.remove(); };
  }, [markers, fullscreen]);
  useEffect(() => {
    const view = map.current;
    if (!view) return;
    marker.current?.remove(); marker.current = null;
    if (value.latitude == null || value.longitude == null || !Number.isFinite(value.latitude) || !Number.isFinite(value.longitude)
      || Math.abs(value.latitude) > 90 || Math.abs(value.longitude) > 180) return;
    const point: L.LatLngTuple = [value.latitude, value.longitude];
    if (onChange) {
      marker.current = L.marker(point, { icon: pinIcon, draggable: true }).addTo(view);
      marker.current.on('dragend', () => {
        const location = marker.current!.getLatLng().wrap();
        latest.current.onChange?.({ ...latest.current.value, latitude: location.lat, longitude: location.lng });
      });
    }
    view.setView(point, 15, { animate: false });
  }, [value.latitude, value.longitude, onChange]);
  return <div className="location-map"><div ref={container} className="map-canvas" role="region" aria-label={onChange ? 'Pick a location on the map' : fullscreen ? 'Public gyms and your private fight locations map' : 'Your fight locations map'}/>
    {tileError && <p className="map-tile-error small muted" role="status">{fullscreen ? 'Map tiles are unavailable. Browse the location lists for addresses and details.' : 'Map tiles are unavailable. Coordinates and text addresses still work.'}</p>}</div>;
}
