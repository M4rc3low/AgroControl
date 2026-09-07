import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Icon } from '../../components/Icon';
import { Badge, Button, Card, EmptyState, Modal } from '../../components/Ui';
import { apiRequest } from '../../lib/api';
import { formatNumber } from '../../lib/format';
import type { Farm } from '../../lib/types';

export function FarmsTab({ farms, reload }: { farms: Farm[]; reload(): Promise<void> }) {
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<Farm | null>(null);
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [area, setArea] = useState('');
  const [city, setCity] = useState('');
  const [state, setState] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const filtered = useMemo(() => farms.filter(farm => `${farm.name} ${farm.city ?? ''} ${farm.state ?? ''}`.toLowerCase().includes(search.toLowerCase())), [farms, search]);

  function showCreate() {
    setEditing(null); setName(''); setArea(''); setCity(''); setState(''); setError(null); setOpen(true);
  }
  function showEdit(farm: Farm) {
    setEditing(farm); setName(farm.name); setArea(String(farm.totalAreaHectares)); setCity(farm.city ?? ''); setState(farm.state ?? ''); setError(null); setOpen(true);
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const totalAreaHectares = Number(area);
    if (!name.trim() || !Number.isFinite(totalAreaHectares) || totalAreaHectares <= 0) { setError('Informe nome e uma área total maior que zero.'); return; }
    setSaving(true); setError(null);
    try {
      const payload = { name: name.trim(), totalAreaHectares, city: city.trim() || null, state: state.trim() || null };
      await apiRequest(editing ? `/api/v1/farms/${editing.id}` : '/api/v1/farms/', { method: editing ? 'PUT' : 'POST', body: JSON.stringify(payload) });
      setOpen(false); await reload();
    } catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível salvar a propriedade.'); }
    finally { setSaving(false); }
  }

  async function deactivate(farm: Farm) {
    if (!window.confirm(`Desativar a propriedade “${farm.name}”?`)) return;
    try { await apiRequest(`/api/v1/farms/${farm.id}`, { method: 'DELETE' }); await reload(); }
    catch (err) { window.alert(err instanceof Error ? err.message : 'Não foi possível desativar.'); }
  }

  return <Card className="resource-card"><div className="resource-toolbar"><div className="search-box"><Icon name="search" size={17} /><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Buscar propriedade, cidade ou estado" aria-label="Buscar propriedades" /></div><Button onClick={showCreate}><Icon name="plus" size={17} /> Nova propriedade</Button></div>{filtered.length ? <div className="table-wrap"><table><thead><tr><th>Propriedade</th><th>Localização</th><th>Área total</th><th>Status</th><th aria-label="Ações" /></tr></thead><tbody>{filtered.map(farm => <tr key={farm.id}><td><strong>{farm.name}</strong><span className="table-sub">ID {farm.id.slice(0, 8)}</span></td><td>{[farm.city, farm.state].filter(Boolean).join(' / ') || '—'}</td><td>{formatNumber(farm.totalAreaHectares)} ha</td><td><Badge tone={farm.isActive ? 'success' : 'neutral'}>{farm.isActive ? 'Ativa' : 'Inativa'}</Badge></td><td><div className="row-actions"><button className="icon-button" aria-label={`Editar ${farm.name}`} onClick={() => showEdit(farm)}><Icon name="edit" size={17} /></button><button className="icon-button icon-button--danger" aria-label={`Desativar ${farm.name}`} onClick={() => void deactivate(farm)}><Icon name="trash" size={17} /></button></div></td></tr>)}</tbody></table></div> : <EmptyState title="Nenhuma propriedade encontrada" description={search ? 'Ajuste a busca ou cadastre uma nova propriedade.' : 'Cadastre a primeira propriedade para iniciar a estrutura produtiva.'} action={<Button onClick={showCreate}><Icon name="plus" size={16} /> Cadastrar propriedade</Button>} />}<Modal open={open} onClose={() => setOpen(false)} title={editing ? 'Editar propriedade' : 'Nova propriedade'} description="A área total deve representar a área cadastrada para gestão nesta propriedade."><form className="form-stack" onSubmit={submit}><label>Nome<input autoFocus required value={name} onChange={event => setName(event.target.value)} placeholder="Fazenda Horizonte" /></label><label>Área total (ha)<input type="number" min="0.01" step="0.01" required value={area} onChange={event => setArea(event.target.value)} placeholder="1250" /></label><div className="form-grid"><label>Cidade<input value={city} onChange={event => setCity(event.target.value)} placeholder="Ribeirão Preto" /></label><label>Estado<input maxLength={40} value={state} onChange={event => setState(event.target.value)} placeholder="SP" /></label></div>{error && <div className="form-error" role="alert">{error}</div>}<div className="modal__actions"><Button type="button" variant="ghost" onClick={() => setOpen(false)}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando…' : 'Salvar propriedade'}</Button></div></form></Modal></Card>;
}
