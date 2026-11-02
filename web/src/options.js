export const STATUSES = ['New', 'Contacted', 'Qualified', 'Unqualified', 'Converted']

export const STATUS_COLORS = {
  New: '#1F4FD8',
  Contacted: '#D97706',
  Qualified: '#0B6E5F',
  Unqualified: '#8A93A0',
  Converted: '#12161C'
}

export const SOURCES = [
  { value: 'Website', label: 'Website' },
  { value: 'Referral', label: 'Referral' },
  { value: 'LinkedIn', label: 'LinkedIn' },
  { value: 'TradeShow', label: 'Trade Show' },
  { value: 'ColdCall', label: 'Cold Call' },
  { value: 'EmailCampaign', label: 'Email Campaign' }
]

export const INDUSTRIES = ['Finance', 'Healthcare', 'Logistics', 'Manufacturing', 'Retail', 'Technology']

export const OWNERS = ['Ana Ruiz', 'Marc Tremblay', 'Priya Nair']

export function sourceLabel(value) {
  return SOURCES.find((source) => source.value === value)?.label ?? value
}
