import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Icon } from '../../components/Icon';
import { Badge, Button, Card, EmptyState, Modal } from '../../components/Ui';
import { apiRequest } from '../../lib/api';
import { formatNumber } from '../../lib/format';
import type { Farm, Field } from '../../lib/types';

export function FieldsTab({ fields, farms, reload }: { fields: Field[]; farms: Farm[]; reload(): Promise<void> }) {
  const [search, setSearch] = useState('');
  const [farmFilter, setFarmFilter] = useState('');
  const [editing, setEditing] = useState<Field | null>(null);
  const [open, setOpen] = useState(false);
  const [farmId, setFarmId] = useState('');
  const [name, setName] = useState('');
  const [area, setArea] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const filtered = useMemo(() => fields.filter(field => (!farmFilter || field.farmId === farmFilter) && field.name.toLowerCase().includes(search.toLowerCase())), [fields, farmFilter, search]);
  const farmName = (id: string) => farms.find(farm => farm.id === id)?.name ?? 'Propriedade não encontrada';

  function showCreate() { setEditing(null); setFarmId(farms[0]?.id ?? ''); setName(''); setArea(''); setError(null); setOpen(true); }
  function showEdit(field: Field) { setEditing(field); setFarmId(field.farmId); setName(field.name); setArea(String(field.areaHectares)); setError(null); setOpen(true); }

  async function submit(event: FormEvent) {
    event.preventDefault(); const areaHectares = Number(area);
    if (!farmId || !name.trim() || !Number.isFinite(areaHectares) || areaHectares <= 0) { setError('Selecione a propriedade, informe o nome e uma área maior que zero.'); return; }
    setSaving(true); setError(null);
    try { const payload = { farmId, name: name.trim(), areaHectares }; await apiRequest(editing ? `/api/v1/fields/${editing.id}` : '/api/v1/fields/', { method: editing ? 'PUT' : 'POST', body: JSON.stringify(payload) }); setOpen(false); await reload(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Não foi possível salvar o talhão.'); }
    finally { setSaving(false); }
  }

  async function deactivate(field: Field) { if (!window.confirm(`Desativar o talhão “${field.name}”?`)) return; try { await apiRequest(`/api/v1/fields/${field.id}`, { method: 'DELETE' }); await reload(); } catch (err) { window.alert(err instanceof Error ? err.message : 'Não foi possível desativar.'); } }

  return <Card className="resource-card"><div className="resource-toolbar resource-toolbar--wrap"><div className="search-box"><Icon name="search" size={17} /><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Buscar talhão" aria-label="Buscar talhões" /></div><select className="filter-select" value={farmFilter} onChange={event => setFarmFilter(event.target.value)} aria-label="Filtrar por propriedade"><option value="">Todas as propriedades</option>{farms.map(farm => <option value={farm.id} key={farm.id}>{farm.name}</option>)}</select><Button onClick={showCreate} disabled={!farms.length}><Icon name="plus" size={17} /> Novo talhão</Button></div>{!farms.length ? <EmptyState title="Cadastre uma propriedade primeiro" description="Todo talhão precisa pertencer a uma propriedade da mesma organização." /> : filtered.length ? <div className="table-wrap"><table><thead><tr><th>Talhão</th><th>Propriedade</th><th>Área</th><th>Status</th><th aria-label="Ações" /></tr></thead><tbody>{filtered.map(field => <tr key={field.id}><td><strong>{field.name}</strong><span className="table-sub">ID {field.id.slice(0, 8)}</span></td><td>{farmName(field.farmId)}</td><td>{formatNumber(field.areaHectares)} ha</td><td><Badge tone={field.isActive ? 'success' : 'neutral'}>{field.isActive ? 'Ativo' : 'Inativo'}</Badge></td><td><div className="row-actions"><button className="icon-button" onClick={() => showEdit(field)} aria-label={`Editar ${field.name}`}><Icon name="edit" size={17} /></button><button className="icon-button icon-button--danger" onClick={() => void deactivate(field)} aria-label={`Desativar ${field.name}`}><Icon name="trash" size={17} /></button></div></td></tr>)}</tbody></table></div> : <EmptyState title="Nenhum talhão encontrado" description="Ajuste os filtros ou cadastre um novo talhão." action={<Button onClick={showCreate}><Icon name="plus" size={16} /> Cadastrar talhão</Button>} />}<Modal open={open} onClose={() => setOpen(false)} title={editing ? 'Editar talhão' : 'Novo talhão'} description="O talhão sempre fica vinculado a uma propriedade da mesma organização."><form className="form-stack" onSubmit={submit}><label>Propriedade<select required value={farmId} onChange={event => setFarmId(event.target.value)}><option value="">Selecione</option>{farms.map(farm => <option value={farm.id} key={farm.id}>{farm.name}</option>)}</select></label><label>Nome<input autoFocus required value={name} onChange={event => setName(event.target.value)} placeholder="Talhão Norte 01" /></label><label>Área (ha)<input type="number" min="0.01" step="0.01" required value={area} onChange={event => setArea(event.target.value)} placeholder="48.5" /></label>{error && <div className="form-error" role="alert">{error}</div>}<div className="modal__actions"><Button type="button" variant="ghost" onClick={() => setOpen(false)}>Cancelar</Button><Button type="submit" disabled={saving}>{saving ? 'Salvando…' : 'Salvar talhão'}</Button></div></form></Modal></Card>;
}
