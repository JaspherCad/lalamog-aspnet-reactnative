import { useEffect, useRef, useState } from 'react';
import { api, errorText } from './api';
import { LocationMap } from './LocationMap';
import type { AreaOption, MapLocation, ReverseGeocodeResult } from './types';

export function LocationPicker({ value, onChange, purpose, onAddressSelected }: { value: MapLocation; onChange: (value: MapLocation) => void; purpose: 'profile' | 'venue' | 'gym'; onAddressSelected?: (address: string) => void }) {
  const current = useRef(value); current.current = value;
  const [regions, setRegions] = useState<AreaOption[]>([]);
  const [provinces, setProvinces] = useState<AreaOption[]>([]);
  const [cities, setCities] = useState<AreaOption[]>([]);
  const [barangays, setBarangays] = useState<AreaOption[]>([]);
  const [filter, setFilter] = useState('');
  const [error, setError] = useState('');
  const [areaError, setAreaError] = useState('');
  const [showMap, setShowMap] = useState(false);
  const [locating, setLocating] = useState(false);
  const [lookingUp, setLookingUp] = useState(false);
  const [lookupError, setLookupError] = useState('');
  const [suggestion, setSuggestion] = useState<(ReverseGeocodeResult & { latitude: number; longitude: number }) | null>(null);
  const lookupRequest = useRef<AbortController | null>(null);
  const canLookup = typeof value.latitude === 'number' && typeof value.longitude === 'number'
    && Number.isFinite(value.latitude) && Number.isFinite(value.longitude) && Math.abs(value.latitude) <= 90 && Math.abs(value.longitude) <= 180;
  const visibleSuggestion = suggestion && suggestion.latitude === value.latitude && suggestion.longitude === value.longitude ? suggestion : null;
  const azureAddress = visibleSuggestion?.features?.find(feature => feature.properties?.address)?.properties?.address;
  const addressDetails: [string, string | null | undefined][] = [
    ['House / building number', visibleSuggestion?.streetNumber], ['Street', visibleSuggestion?.road],
    ['Neighbourhood', visibleSuggestion?.suburb], ['City / locality', visibleSuggestion?.city],
    ['Administrative areas', azureAddress?.adminDistricts?.map(area => area.name).filter(Boolean).join(' › ')],
    ['Postal code', visibleSuggestion?.postalCode], ['Country', visibleSuggestion?.country],
    ['Country code', visibleSuggestion?.countryCode], ['Match confidence', visibleSuggestion?.confidence],
    ['Location type', visibleSuggestion?.resultType?.replace(/([a-z])([A-Z])/g, '$1 $2')],
  ];
  useEffect(() => {
    setSuggestion(null); setLookupError(''); setLookingUp(false);
    return () => { lookupRequest.current?.abort(); lookupRequest.current = null; };
  }, [value.latitude, value.longitude]);
  async function findAddress() {
    if (!canLookup || lookingUp) return;
    const latitude = value.latitude!; const longitude = value.longitude!;
    const controller = new AbortController(); lookupRequest.current = controller;
    setLookingUp(true); setLookupError(''); setSuggestion(null);
    try {
      const result = await api<ReverseGeocodeResult>('/maps/reverse', { method: 'POST', body: JSON.stringify({ latitude, longitude }), signal: controller.signal });
      if (!controller.signal.aborted && current.current.latitude === latitude && current.current.longitude === longitude) setSuggestion({ ...result, latitude, longitude });
    } catch (err) {
      if (!controller.signal.aborted) setLookupError(errorText(err));
    } finally {
      if (lookupRequest.current === controller) { setLookingUp(false); lookupRequest.current = null; }
    }
  }
  useEffect(() => {
    const controller = new AbortController();
    void api<AreaOption[]>('/areas/regions', { signal: controller.signal }).then(setRegions).catch(err => { if (!controller.signal.aborted) setAreaError(errorText(err)); });
    return () => controller.abort();
  }, []);
  useEffect(() => {
    const controller = new AbortController(); setProvinces([]); setCities([]);
    if (value.regionCode) {
      const region = encodeURIComponent(value.regionCode);
      void Promise.all([
        api<AreaOption[]>(`/areas/provinces?regionCode=${region}`, { signal: controller.signal }),
        api<AreaOption[]>(`/areas/cities?regionCode=${region}&provinceCode=${encodeURIComponent(value.provinceCode || 'none')}`, { signal: controller.signal })
      ]).then(([p, c]) => { setProvinces(p); setCities(c); setAreaError(''); }).catch(err => { if (!controller.signal.aborted) setAreaError(errorText(err)); });
    }
    return () => controller.abort();
  }, [value.regionCode, value.provinceCode]);
  useEffect(() => {
    const controller = new AbortController(); setBarangays([]); setFilter('');
    if (value.cityCode) void api<AreaOption[]>(`/areas/barangays?cityCode=${encodeURIComponent(value.cityCode)}`, { signal: controller.signal })
      .then(setBarangays).catch(err => { if (!controller.signal.aborted) setAreaError(errorText(err)); });
    return () => controller.abort();
  }, [value.cityCode]);
  function locate() {
    if (!navigator.geolocation) { setError('Location is not supported here. Enter coordinates or use the map.'); return; }
    setLocating(true); setError('');
    navigator.geolocation.getCurrentPosition(position => {
      setLocating(false); onChange({ ...current.current, latitude: position.coords.latitude, longitude: position.coords.longitude }); setShowMap(true);
    }, () => { setLocating(false); setError('Location permission was denied or timed out. You can still use the map or enter coordinates.'); }, { timeout: 10000, maximumAge: 60000 });
  }
  const visibleBarangays = barangays.filter(area => area.name.toLocaleLowerCase().includes(filter.toLocaleLowerCase()) || area.code === value.barangayCode);
  return <fieldset className="location-picker"><legend>{purpose === 'profile' ? 'Your area and private pin' : purpose === 'gym' ? 'Gym area and public pin' : 'Venue area and map pin'}</legend>
    <p className="small muted">{purpose === 'profile' ? 'Your public card shows the city. Your barangay and exact pin are private.' : purpose === 'gym' ? 'This gym address and pin will be public when you publish the listing.' : 'Only you and your matched partner can see this venue pin.'} Select an area and place the pin yourself. You can look up a suggested address or enter it manually.</p>
    <div className="form-grid">
      <label>Region<select aria-label="Region" value={value.regionCode || ''} onChange={e => onChange({ ...value, regionCode: e.target.value || null, provinceCode: null, cityCode: null, barangayCode: null, psgcVersion: null })}>
        <option value="">Use text location only</option>{regions.map(area => <option key={area.code} value={area.code}>{area.name}</option>)}
      </select></label>
      {value.regionCode && <label>Province / independent city<select aria-label="Province / independent city" value={value.provinceCode || 'none'} onChange={e => onChange({ ...value, provinceCode: e.target.value === 'none' ? null : e.target.value, cityCode: null, barangayCode: null, psgcVersion: null })}>
        <option value="none">Independent city / no province</option>{provinces.map(area => <option key={area.code} value={area.code}>{area.name}</option>)}
      </select></label>}
      {value.regionCode && <label>City / municipality<select aria-label="City / municipality" value={value.cityCode || ''} required onChange={e => onChange({ ...value, cityCode: e.target.value || null, barangayCode: null, psgcVersion: null })}>
        <option value="">Choose a city / municipality</option>{cities.map(area => <option key={area.code} value={area.code}>{area.name}</option>)}
      </select></label>}
      {value.cityCode && <div className="barangay-field"><label>Find a barangay<input aria-label="Find a barangay" value={filter} onChange={e => setFilter(e.target.value)} placeholder="Filter by name"/></label>
        <label>Barangay<select aria-label="Barangay" value={value.barangayCode || ''} required={purpose !== 'gym'} onChange={e => onChange({ ...value, barangayCode: e.target.value || null, psgcVersion: null })}>
          <option value="">Choose a barangay</option>{visibleBarangays.map(area => <option key={area.code} value={area.code}>{area.name}</option>)}
        </select></label></div>}
    </div>
    {areaError && <p className="small muted">{areaError} Use the text location or clear the area selection to enter it manually.</p>}
    {error && <p className="small error" role="alert">{error} Manual addresses and coordinates remain available.</p>}
    <div className="actions"><button type="button" className="secondary" disabled={locating} onClick={locate}>{locating ? 'Finding location…' : 'Use my location'}</button><button type="button" className="secondary" onClick={() => setShowMap(!showMap)}>{showMap ? 'Hide map' : 'Show map'}</button>
      <button type="button" className="link" onClick={() => onChange({})}>Clear area and pin</button></div>
    <div className="form-grid coordinates"><label>Latitude<input aria-label="Latitude" type="number" step="any" min={-90} max={90} value={value.latitude ?? ''} onChange={e => onChange({ ...value, latitude: e.target.value === '' ? null : Number(e.target.value) })}/></label>
      <label>Longitude<input aria-label="Longitude" type="number" step="any" min={-180} max={180} value={value.longitude ?? ''} onChange={e => onChange({ ...value, longitude: e.target.value === '' ? null : Number(e.target.value) })}/></label></div>
    {showMap && <LocationMap value={value} onChange={onChange}/>}
    <div className="address-lookup">
      <button type="button" className="secondary" disabled={!canLookup || lookingUp} onClick={() => void findAddress()}>{lookingUp ? 'Finding address…' : 'Find address for this pin'}</button>
      <p className="small muted">This sends the pin to Azure Maps to find an address. Look up public meeting places only; keep home and confidential locations private.</p>
      {lookupError && <p className="small error" role="alert">{lookupError} You can still enter the address manually.</p>}
      {visibleSuggestion && <div className="suggested-address" role="status"><strong>Suggested address</strong><p>{visibleSuggestion.address}</p>
        <p className="small muted">Map data may be incomplete. Review it before using it. This does not select an official PSGC barangay.</p>
        <details className="address-details"><summary>View address details</summary>
          <dl>{addressDetails.filter(([, detail]) => detail).map(([label, detail]) => <div key={label}><dt>{label}</dt><dd>{detail}</dd></div>)}</dl>
          <p className="small muted">Only available details are shown. The suggested address may refer to a nearby street or area.</p>
        </details>
        {onAddressSelected && visibleSuggestion.address.length > 300 && <p className="small muted">The venue address fits 300 characters. Review the shortened text after copying.</p>}
        {onAddressSelected && <button type="button" className="secondary" onClick={() => onAddressSelected(visibleSuggestion.address.slice(0, 300))}>Use this address</button>}
      </div>}
      <small className="muted">Address lookup: <a href="https://azure.microsoft.com/products/azure-maps/" target="_blank" rel="noreferrer">Azure Maps</a></small>
    </div>
    <small className="muted">Area names: <a href="https://psa.gov.ph/classification/psgc/" target="_blank" rel="noreferrer">Philippine Statistics Authority</a>, PSGC 2026 Q3. Map tiles load from OpenStreetMap when opened.</small>
  </fieldset>;
}
