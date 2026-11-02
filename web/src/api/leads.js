export async function listLeads(filters, signal) {
  const query = new URLSearchParams()

  for (const [key, value] of Object.entries(filters)) {
    if (value !== '' && value !== null && value !== undefined) {
      query.set(key, value)
    }
  }

  const response = await fetch(`/api/leads?${query}`, { signal })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  return response.json()
}

export async function createLead(lead) {
  const response = await fetch('/api/leads', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(lead)
  })

  if (response.ok) {
    return { ok: true, lead: await response.json() }
  }

  const problem = await response.json().catch(() => null)
  return { ok: false, status: response.status, problem }
}
