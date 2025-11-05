import { useEffect, useState } from 'react'

export default function App() {
  const [msg, setMsg] = useState('loading...')
  useEffect(() => {
    fetch('/api/hello').then(r => r.json())
      .then(d => setMsg(d.message))
      .catch(() => setMsg('API error'))
  }, [])
  return (
    <div style={{ fontFamily: 'Inter, system-ui, sans-serif', padding: 24 }}>
      <h1>MyApp (React + .NET 9)</h1>
      <p>API says: <b>{msg}</b></p>
    </div>
  )
}
