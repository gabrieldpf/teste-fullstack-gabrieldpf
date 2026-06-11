import React, { useState } from 'react'

export default function CsvUploadPage(){
  const [log, setLog] = useState(null)
  const [erro, setErro] = useState(null)
  const [carregando, setCarregando] = useState(false)

  async function handleUpload(e){
    e.preventDefault()
    setErro(null)
    setCarregando(true)
    try {
      const file = e.target.file.files[0]
      if (!file) {
        setErro('Selecione um arquivo CSV.')
        return
      }
      const fd = new FormData()
      fd.append('file', file)
      const r = await fetch((import.meta.env.VITE_API_URL || 'http://localhost:5000') + '/api/import/csv', {
        method: 'POST',
        body: fd
      })
      const j = await r.json()
      if (!r.ok) {
        setErro(j.mensagem || 'Falha na importação.')
        setLog(null)
        return
      }
      setLog(j)
    } catch (err) {
      setErro(err.message)
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div>
      <h2>Importar CSV</h2>

      {erro && <div className="alert alert-error">{erro}</div>}

      <div className="section">
        <form onSubmit={handleUpload} style={{display:'flex', gap:10, alignItems:'center'}}>
          <input type="file" name="file" accept=".csv" />
          <button type="submit" disabled={carregando}>{carregando ? 'Enviando...' : 'Enviar'}</button>
        </form>
        <p className="note">
          Colunas esperadas: placa, modelo, ano, cliente_identificador, cliente_nome,
          cliente_telefone, cliente_endereco, mensalista, valor_mensalidade
        </p>
      </div>

      <h3 style={{marginTop:16}}>Relatório</h3>
      <div className="section">
        {!log ? (
          <p className="note">Aguardando upload...</p>
        ) : (
          <>
            <div className="stats-row">
              <span className="stat"><strong>{log.processados}</strong> processadas</span>
              <span className="stat stat-ok"><strong>{log.inseridos}</strong> inseridas</span>
              <span className="stat stat-err"><strong>{log.erros}</strong> com erro</span>
            </div>

            {log.detalhes?.length > 0 ? (
              <table style={{marginTop:12}}>
                <thead>
                  <tr><th>Linha</th><th>Motivo</th><th>Conteúdo</th></tr>
                </thead>
                <tbody>
                  {log.detalhes.map((e, i) => (
                    <tr key={i}>
                      <td>{e.linha}</td>
                      <td className="text-error">{e.motivo}</td>
                      <td><code className="raw-line">{e.conteudo}</code></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <p className="note" style={{marginTop:12}}>Nenhum erro encontrado.</p>
            )}
          </>
        )}
      </div>
    </div>
  )
}
