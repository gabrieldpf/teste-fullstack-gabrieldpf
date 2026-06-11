import React, { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, apiDelete } from '../api'

const formVazio = { nome:'', telefone:'', endereco:'', mensalista:false, valorMensalidade:'' }

function toPayload(form){
  return {
    nome: form.nome.trim(),
    telefone: form.telefone.trim() || null,
    endereco: form.endereco.trim() || null,
    mensalista: form.mensalista,
    valorMensalidade: form.valorMensalidade ? Number(form.valorMensalidade) : null
  }
}

export default function ClientesPage(){
  const qc = useQueryClient()
  const [filtro, setFiltro] = useState('')
  const [mensalista, setMensalista] = useState('all')
  const [form, setForm] = useState(formVazio)
  const [editando, setEditando] = useState(null)
  const [erro, setErro] = useState(null)

  const q = useQuery({
    queryKey:['clientes', filtro, mensalista],
    queryFn:() => apiGet(`/api/clientes?pagina=1&tamanho=20&filtro=${encodeURIComponent(filtro)}&mensalista=${mensalista}`)
  })

  const create = useMutation({
    mutationFn: (data) => apiPost('/api/clientes', data),
    onSuccess: () => {
      setForm(formVazio)
      setErro(null)
      qc.invalidateQueries({ queryKey:['clientes'] })
    },
    onError: (e) => setErro(e.message)
  })

  const update = useMutation({
    mutationFn: ({ id, data }) => apiPut(`/api/clientes/${id}`, data),
    onSuccess: () => {
      setEditando(null)
      setErro(null)
      qc.invalidateQueries({ queryKey:['clientes'] })
    },
    onError: (e) => setErro(e.message)
  })

  const remover = useMutation({
    mutationFn: (id) => apiDelete(`/api/clientes/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey:['clientes'] }),
    onError: (e) => setErro(e.message)
  })

  function iniciarEdicao(c){
    setEditando(c.id)
    setErro(null)
    setForm({
      nome: c.nome,
      telefone: c.telefone || '',
      endereco: c.endereco || '',
      mensalista: c.mensalista,
      valorMensalidade: c.valorMensalidade ?? ''
    })
  }

  function cancelarEdicao(){
    setEditando(null)
    setForm(formVazio)
    setErro(null)
  }

  return (
    <div>
      <h2>Clientes</h2>

      {erro && <div className="alert alert-error">{erro}</div>}

      <div className="section">
        <div className="grid grid-3">
          <input placeholder="Buscar por nome" value={filtro} onChange={e=>setFiltro(e.target.value)} />
          <select value={mensalista} onChange={e=>setMensalista(e.target.value)}>
            <option value="all">Todos</option>
            <option value="true">Mensalistas</option>
            <option value="false">Não mensalistas</option>
          </select>
          <div/>
        </div>
      </div>

      <h3>{editando ? 'Editar cliente' : 'Novo cliente'}</h3>
      <div className="section">
        <div className="grid grid-4">
          <input placeholder="Nome *" value={form.nome} onChange={e=>setForm({...form, nome:e.target.value})}/>
          <input placeholder="Telefone" value={form.telefone} onChange={e=>setForm({...form, telefone:e.target.value})}/>
          <input placeholder="Endereço" value={form.endereco} onChange={e=>setForm({...form, endereco:e.target.value})}/>
          <label className="checkbox-label">
            <input type="checkbox" checked={form.mensalista} onChange={e=>setForm({...form, mensalista:e.target.checked})}/> Mensalista
          </label>
          <input placeholder="Valor mensalidade" value={form.valorMensalidade} onChange={e=>setForm({...form, valorMensalidade:e.target.value})}/>
          <div/>
          <div style={{display:'flex', gap:8}}>
            {editando ? (
              <>
                <button onClick={()=>update.mutate({ id: editando, data: toPayload(form) })}>Salvar</button>
                <button className="btn-ghost" onClick={cancelarEdicao}>Cancelar</button>
              </>
            ) : (
              <button onClick={()=>create.mutate(toPayload(form))}>Salvar</button>
            )}
          </div>
        </div>
      </div>

      <h3 style={{marginTop:16}}>Lista</h3>
      <div className="section">
        {q.isLoading? <p>Carregando...</p> : (
          <table>
            <thead>
              <tr>
                <th>Nome</th><th>Telefone</th><th>Endereço</th>
                <th>Mensalista</th><th>Mensalidade</th><th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {q.data.itens.map(c=>(
                <tr key={c.id}>
                  <td>{c.nome}</td>
                  <td>{c.telefone || '-'}</td>
                  <td>{c.endereco || '-'}</td>
                  <td>{c.mensalista? 'Sim':'Não'}</td>
                  <td>{c.valorMensalidade != null ? `R$ ${Number(c.valorMensalidade).toFixed(2)}` : '-'}</td>
                  <td style={{display:'flex', gap:8}}>
                    <button className="btn-ghost" onClick={()=>iniciarEdicao(c)}>Editar</button>
                    <button className="btn-ghost" onClick={()=>remover.mutate(c.id)}>Excluir</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
