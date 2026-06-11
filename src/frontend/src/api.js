
const API = import.meta.env.VITE_API_URL || 'http://localhost:5000'

async function parseError(r){
  const text = await r.text()
  try {
    const json = JSON.parse(text)
    return json.mensagem || json.message || text
  } catch {
    return text || `Erro ${r.status}`
  }
}

export async function apiGet(path){
  const r = await fetch(API + path)
  if(!r.ok) throw new Error(await parseError(r))
  return r.json()
}

export async function apiPost(path, body){
  const r = await fetch(API + path, {
    method:'POST',
    headers:{'Content-Type':'application/json'},
    body: JSON.stringify(body)
  })
  if(!r.ok) throw new Error(await parseError(r))
  return r.json()
}

export async function apiPut(path, body){
  const r = await fetch(API + path, {
    method:'PUT',
    headers:{'Content-Type':'application/json'},
    body: JSON.stringify(body)
  })
  if(!r.ok) throw new Error(await parseError(r))
  return r.json()
}

export async function apiDelete(path){
  const r = await fetch(API + path, { method:'DELETE' })
  if(!r.ok) throw new Error(await parseError(r))
  if (r.status === 204) return null
  const text = await r.text()
  return text || null
}
