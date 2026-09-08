import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Icon } from '../../components/Icon';
import { Badge, Button, Card, EmptyState, Modal } from '../../components/Ui';
import { apiRequest } from '../../lib/api';
import { useFarmScope } from '../../lib/farmScope';
import { formatNumber } from '../../lib/format';
import type { Farm } from '../../lib/types';

const brazilianStates = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO'];
const brazilianTimeZones = [
  ['America/Sao_Paulo', 'Brasília / São Paulo'],
  ['America/Cuiaba', 'Cuiabá'],
  ['America/Manaus', 'Manaus'],
  ['America/Rio_Branco', 'Rio Branco'],
  ['America/Noronha', 'Fernando de Noronha']
] as const;

export function FarmsTab({ farms, reload }: { farms: Farm[]; reload(): Promise<void> }) {
  const { access, regions, refresh: refreshFarmScope } = useFarmScope();
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<Farm | null>(null);
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [area, setArea] = useState('');
  const [city, setCity] = useState('');
  const [stateCode, setStateCode] = useState('');
  const [countryCode, setCountryCode] = useState('BR');
  const [regionId, setRegionId] = useState('');
  const [municipalityCode, setMunicipalityCode] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [latitude, setLatitude] = useState('');
  const [longitude, setLongitude] = useState('');
  const [timeZoneId, setTimeZoneId] = useState('America/Sao_Paulo');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const canManageFarmStructure = Boolean(access?.allFarms);
  const regionNames = useMemo(() => Object.fromEntries(regions.map(region => [region.id, region.name])), [regions]);
  const filtered = useMemo(() => farms.filter(farm => `${farm.name} ${farm.city ?? ''} ${farm.stateCode ?? farm.state ?? ''} ${farm.countryCode ?? ''}`.toLowerCase().includes(search.toLowerCase())), [farms, search]);

  function resetLocation() {
    setCountryCode('BR'); setRegionId(''); setMunicipalityCode(''); setPostalCode(''); setLatitude(''); setLongitude(''); setTimeZoneId('America/Sao_Paulo');
  }

  function showCreate() {
    setEditing(null); setName(''); setArea(''); setCity(''); setStateCode(''); resetLocation(); setError(null); setOpen(true);
  }

  function showEdit(farm: Farm) {
    setEditing(farm);
    setName(farm.name);
    setArea(String(farm.totalAreaHectares));
    setCity(farm.city ?? '');
    setStateCode(farm.stateCode ?? farm.state ?? '');
    setCountryCode(farm.countryCode ?? 'BR');
    setRegionId(farm.operationalRegionId ?? '');
    setMunicipalityCode(farm.municipalityCode ?? '');
    setPostalCode(farm.postalCode ?? '');
    setLatitude(farm.latitude == null ? '' : String(farm.latitude));
    setLongitude(farm.longitude == null ? '' : String(farm.longitude));
    setTimeZoneId(farm.timeZoneId ?? 'America/Sao_Paulo');
    setError(null); setOpen(true);
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const totalAreaHectares = Number(area);
    const parsedLatitude = latitude.trim() ? Number(latitude) : null;
    const parsedLongitude = longitude.trim() ? Number(longitude) : null;
    if (!name.trim() || !Number.isFinite(totalAreaHectares) || totalAreaHectares <= 0) { setError('Informe nome e uma área total maior que zero.'); return; }
    if (countryCode.trim().toUpperCase() === 'BR' && stateCode && !brazilianStates.includes(stateCode.toUpperCase())) { setError('Informe uma UF brasileira válida.'); return; }
    if (parsedLatitude != null && (!Number.isFinite(parsedLatitude) || parsedLatitude < -90 || parsedLatitude > 90)) { setError('Latitude deve estar entre -90 e 90.'); return; }
    if (parsedLongitude != null && (!Number.isFinite(parsedLongitude) || parsedLongitude < -180 || parsedLongitude > 180)) { setError('Longitude deve estar entre -180 e 180.'); return; }

    setSaving(true); setError(null);
    try {
      const normalizedState = stateCode.trim().toUpperCase() || null;
      const payload = {
        name: name.trim(),
        totalAreaHectares,
        city: city.trim() || null,
        state: normalizedState,
        operationalRegionId: regionId || null,
        countryCode: countryCode.trim().toUpperCase() || 'BR',
        stateCode: normalizedState,
        municipalityCode: municipalityCode.trim() || null,
        postalCode: postalCode.trim() || null,
        latitude: parsedLatitude,
        longitude: parsedLongitude,
        timeZoneId
      };
      await apiRequest(editing ? `/api/v1/farms/${editing.id}` : '/api/v1/farms/', { method: editing ? 'PUT' : 'POST', body: JSON.stringify(payload) });
      setOpen(false);
      await Promise.all([reload(), refreshFarmScope()]);
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível salvar a propriedade.'); }
    finally { setSaving(false); }
  }

  async function deactivate(farm: Farm) {
    if (!window.confirm(`Desativar a propriedade “${farm.name}”?`)) return;
    try { await apiRequest(`/api/v1/farms/${farm.id}`, { method: 'DELETE' }); await Promise.all([reload(), refreshFarmScope()]); }
    catch (err) { window.alert(err instanceof Error ? err.message : 'Não foi possível desativar.'); }
  }

  return <Card className="resource-card">
    <div className="resource-toolbar">
      <div className="search-box"><Icon name="search" size={17} /><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Buscar propriedade, cidade ou UF" aria-label="Buscar propriedades" /></div>
      <Button onClick={showCreate} disabled={!canManageFarmStructure} title={canManageFarmStructure ? undefined : 'Criar propriedades exige acesso a todas as fazendas'}><Icon name="plus" size={17} /> Nova propriedade</Button>
    </div>
    {filtered.length ? <div className="table-wrap"><table><thead><tr><th>Propriedade</th><th>Região / localização</th><th>Área total</th><th>Fuso</th><th>Status</th><th aria-label="Ações" /></tr></thead><tbody>{filtered.map(farm => <tr key={farm.id}><td><strong>{farm.name}</strong><span className="table-sub">ID {farm.id.slice(0, 8)}</span></td><td><strong>{farm.operationalRegionId ? regionNames[farm.operationalRegionId] ?? 'Região' : 'Sem região'}</strong><span className="table-sub">{[farm.city, farm.stateCode ?? farm.state, farm.countryCode].filter(Boolean).join(' / ') || '—'}</span></td><td>{formatNumber(farm.totalAreaHectares)} ha</td><td><span className="table-sub">{farm.timeZoneId}</span></td><td><Badge tone={farm.isActive ? 'success' : 'neutral'}>{farm.isActive ? 'Ativa' : 'Inativa'}</Badge></td><td><div className="row-actions"><button className="icon-button" aria-label={`Editar ${farm.name}`} onClick={() => showEdit(farm)}><Icon name="edit" size={17} /></button><button className="icon-button icon-button--danger" aria-label={`Desativar ${farm.name}`} onClick={() => void deactivate(farm)}><Icon name="trash" size={17} /></button></div></td></tr>)}</tbody></table></div> : <EmptyState title="Nenhuma propriedade encontrada" description={search ? 'Ajuste a busca ou cadastre uma nova propriedade.' : 'Cadastre a primeira propriedade para iniciar a estrutura produtiva.'} action={canManageFarmStructure ? <Button onClick={showCreate}><Icon name="plus" size={16} /> Cadastrar propriedade</Button> : undefined} />}

    <Modal open={open} onClose={() => setOpen(false)} title={editing ? 'Editar propriedade' : 'Nova propriedade'} description="Localização, região e fuso são usados para organizar operações em diferentes estados sem criar outro tenant.">
      <form className="form-stack" onSubmit={submit}>
        <label>Nome<input autoFocus required value={name} onChange={event => setName(event.target.value)} placeholder="Fazenda Horizonte" /></label>
        <div className="form-grid"><label>Área total (ha)<input type="number" min="0.01" step="0.01" required value={area} onChange={event => setArea(event.target.value)} placeholder="1250" /></label><label>Região operacional<select value={regionId} onChange={event => setRegionId(event.target.value)} disabled={!canManageFarmStructure}><option value="">Sem região</option>{regions.filter(region => region.isActive).map(region => <option key={region.id} value={region.id}>{region.name}</option>)}</select></label></div>
        <div className="form-grid"><label>País (ISO)<input maxLength={2} value={countryCode} onChange={event => setCountryCode(event.target.value.toUpperCase())} placeholder="BR" /></label><label>UF<select value={stateCode} onChange={event => setStateCode(event.target.value)}><option value="">Selecione</option>{brazilianStates.map(uf => <option key={uf} value={uf}>{uf}</option>)}</select></label></div>
        <div className="form-grid"><label>Município<input value={city} onChange={event => setCity(event.target.value)} placeholder="Rio Verde" /></label><label>Código do município<input value={municipalityCode} onChange={event => setMunicipalityCode(event.target.value)} placeholder="IBGE opcional" /></label></div>
        <div className="form-grid"><label>CEP<input value={postalCode} onChange={event => setPostalCode(event.target.value)} placeholder="75900-000" /></label><label>Fuso horário<select value={timeZoneId} onChange={event => setTimeZoneId(event.target.value)}>{brazilianTimeZones.map(([value, label]) => <option key={value} value={value}>{label} · {value}</option>)}</select></label></div>
        <div className="form-grid"><label>Latitude<input type="number" step="0.000001" value={latitude} onChange={event => setLatitude(event.target.value)} placeholder="-17.7923" /></label><label>Longitude<input type="number" step="0.000001" value={longitude} onChange={event => setLongitude(event.target.value)} placeholder="-50.9192" /></label></div>
        {error && <div className="form-error" role="alert">{error}</div>}
        <div className="modal__actions"><Button type="button" variant="ghost" onClick={() => setOpen(false)}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando…' : 'Salvar propriedade'}</Button></div>
      </form>
    </Modal>
  </Card>;
}
