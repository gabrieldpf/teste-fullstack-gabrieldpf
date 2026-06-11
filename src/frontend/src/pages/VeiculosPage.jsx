import React, { useEffect, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, apiDelete } from '../api'

const formVazio = { placa:'', modelo:'', ano:'', clienteId:'' }

export default function VeiculosPage(){
  const qc = useQueryClient()
  const [clienteId, setClienteId] = useState('')
  const [form, setForm] = useState(formVazio)
  const [editando, setEditando] = useState(null)
  const [erro, setErro] = useState(null)

  const clientes = useQuery({
    queryKey:['clientes-mini'],
    queryFn:() => apiGet('/api/clientes?pagina=1&tamanho=100')
  })

  const veiculos = useQuery({
    queryKey:['veiculos', clienteId],
    queryFn:() => apiGet(`/api/veiculos${clienteId?`?clienteId=${clienteId}`:''}`)
  })

  const create = useMutation({
    mutationFn: (data) => apiPost('/api/veiculos', data),
    onSuccess: () => {
      setForm(f => ({...formVazio, clienteId: f.clienteId || clienteId}))
      setErro(null)
      qc.invalidateQueries({ queryKey:['veiculos'] })
    },
    onError: (e) => setErro(e.message)
  })

  const update = useMutation({
    mutationFn: ({id, data}) => apiPut(`/api/veiculos/${id}`, data),
    onSuccess: () => {
      setEditando(null)
      setErro(null)
      qc.invalidateQueries({ queryKey:['veiculos'] })
    },
    onError: (e) => setErro(e.message)
  })

  const remover = useMutation({
    mutationFn: (id) => apiDelete(`/api/veiculos/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey:['veiculos'] }),
    onError: (e) => setErro(e.message)
  })

  useEffect(()=>{
    if(clientes.data?.itens?.length && !clienteId){
      setClienteId(clientes.data.itens[0].id)
      setForm(f => ({...f, clienteId: clientes.data.itens[0].id}))
    }
  }, [clientes.data, clienteId])

  function nomeCliente(id){
    return clientes.data?.itens?.find(c => c.id === id)?.nome || id
  }

  function iniciarEdicao(v){
    setEditando(v.id)
    setErro(null)
    setForm({
      placa: v.placa,
      modelo: v.modelo || '',
      ano: v.ano ?? '',
      clienteId: v.clienteId
    })
  }

  function cancelarEdicao(){
    setEditando(null)
    setForm({...formVazio, clienteId: clienteId})
    setErro(null)
  }

  function salvar(){
    const payload = {
      placa: form.placa.trim(),
      modelo: form.modelo.trim() || null,
      ano: form.ano ? Number(form.ano) : null,
      clienteId: form.clienteId || clienteId
    }
    if(editando) update.mutate({ id: editando, data: payload })
    else create.mutate(payload)
  }

  return (
    <div>
      <h2>Veículos</h2>

      {erro && <div className="alert alert-error">{erro}</div>}

      <div className="section">
        <div style={{display:'flex', gap:10, alignItems:'center'}}>
          <label>Filtrar por cliente: </label>
          <select value={clienteId} onChange={e=>setClienteId(e.target.value)} style={{maxWidth:280}}>
            <option value="">Todos</option>
            {clientes.data?.itens?.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
      </div>

      <h3>{editando ? 'Editar veículo' : 'Novo veículo'}</h3>
      <div className="section">
        <div className="grid grid-4">
          <input placeholder="Placa *" value={form.placa} onChange={e=>setForm({...form, placa:e.target.value})}/>
          <input placeholder="Modelo" value={form.modelo} onChange={e=>setForm({...form, modelo:e.target.value})}/>
          <input placeholder="Ano" type="number" value={form.ano} onChange={e=>setForm({...form, ano:e.target.value})}/>
          <select value={form.clienteId || clienteId} onChange={e=>setForm({...form, clienteId:e.target.value})}>
            {clientes.data?.itens?.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
          <div style={{display:'flex', gap:8}}>
            {editando ? (
              <>
                <button onClick={salvar}>Salvar</button>
                <button className="btn-ghost" onClick={cancelarEdicao}>Cancelar</button>
              </>
            ) : (
              <button onClick={salvar}>Salvar</button>
            )}
          </div>
        </div>
      </div>

      <h3 style={{marginTop:16}}>Lista</h3>
      <div className="section">
        {veiculos.isLoading? <p>Carregando...</p> : (
          <table>
            <thead>
              <tr><th>Placa</th><th>Modelo</th><th>Ano</th><th>Cliente</th><th>Ações</th></tr>
            </thead>
            <tbody>
              {veiculos.data?.map(v=>(
                <tr key={v.id}>
                  <td>{v.placa}</td>
                  <td>{v.modelo || '-'}</td>
                  <td>{v.ano ?? '-'}</td>
                  <td>{nomeCliente(v.clienteId)}</td>
                  <td style={{display:'flex', gap:8}}>
                    <button className="btn-ghost" onClick={()=>iniciarEdicao(v)}>Editar</button>
                    <button className="btn-ghost" onClick={()=>remover.mutate(v.id)}>Excluir</button>
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
