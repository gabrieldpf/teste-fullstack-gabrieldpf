import React, { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost } from '../api'

export default function FaturamentoPage(){
  const [comp, setComp] = useState('2025-08')
  const [erro, setErro] = useState(null)
  const [gerando, setGerando] = useState(false)

  const faturas = useQuery({
    queryKey:['faturas', comp],
    queryFn:() => apiGet(`/api/faturas?competencia=${comp}`)
  })

  async function gerar(){
    setErro(null)
    setGerando(true)
    try {
      await apiPost('/api/faturas/gerar', { competencia: comp })
      await faturas.refetch()
    } catch (e) {
      setErro(e.message)
    } finally {
      setGerando(false)
    }
  }

  return (
    <div>
      <h2>Faturamento</h2>

      {erro && <div className="alert alert-error">{erro}</div>}

      <div className="section">
        <div style={{display:'flex', gap:10, alignItems:'center'}}>
          <input value={comp} onChange={e=>setComp(e.target.value)} placeholder="yyyy-MM" style={{maxWidth:140}} />
          <button onClick={gerar} disabled={gerando}>{gerando ? 'Gerando...' : 'Gerar faturas'}</button>
        </div>
        <p className="note">Faturamento proporcional: cobra apenas os dias em que o cliente teve veículos associados no mês.</p>
      </div>

      <h3 style={{marginTop:16}}>Faturas</h3>
      <div className="section">
        {faturas.isLoading? <p>Carregando...</p> : (
          <table>
            <thead>
              <tr><th>Cliente</th><th>Competência</th><th>Valor</th><th>Veículos</th><th>Observação</th><th>Placas</th></tr>
            </thead>
            <tbody>
              {faturas.data?.length === 0 && (
                <tr><td colSpan={6} className="note">Nenhuma fatura para esta competência.</td></tr>
              )}
              {faturas.data?.map(f=>(<FaturaRow key={f.id} f={f} />))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}

function FaturaRow({f}){
  const [show, setShow] = useState(false)
  const [placas, setPlacas] = useState([])

  return (
    <tr>
      <td>{f.clienteNome || f.clienteId}</td>
      <td>{f.competencia}</td>
      <td>R$ {Number(f.valor).toFixed(2)}</td>
      <td>{f.qtdVeiculos}</td>
      <td>{f.observacao || '-'}</td>
      <td>
        <button className="btn-ghost" onClick={async ()=>{
          if(!show){
            const r = await apiGet(`/api/faturas/${f.id}/placas`)
            setPlacas(r)
          }
          setShow(s=>!s)
        }}>{show?'Ocultar':'Detalhar'}</button>
        {show && <div style={{marginTop:6}}>{placas.join(', ')}</div>}
      </td>
    </tr>
  )
}
