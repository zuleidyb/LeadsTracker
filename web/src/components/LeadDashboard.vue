<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { listLeads } from '../api/leads'
import { INDUSTRIES, OWNERS, SOURCES, STATUSES, STATUS_COLORS, sourceLabel } from '../options'

const PAGE_SIZE = 10
const DASH = '\u2014'

const emptyFilters = () => ({ q: '', status: '', source: '', industry: '', owner: '', from: '', to: '' })

const filters = reactive(emptyFilters())
const page = ref(1)
const result = ref({ items: [], totalCount: 0, statusCounts: {} })
const loading = ref(true)
const error = ref('')

const money = new Intl.NumberFormat('en-CA', { style: 'currency', currency: 'CAD', maximumFractionDigits: 0 })
const dates = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' })

let controller = null
let timer = null

async function load() {
  controller?.abort()
  const current = new AbortController()
  controller = current
  loading.value = true
  error.value = ''

  try {
    result.value = await listLeads({ ...filters, page: page.value, pageSize: PAGE_SIZE }, current.signal)
  } catch (failure) {
    if (failure.name !== 'AbortError') {
      error.value = 'Could not load leads. Check that the API is running.'
    }
  } finally {
    if (current === controller) {
      loading.value = false
    }
  }
}

function goToPage(next) {
  page.value = next
  load()
}

function clearFilters() {
  Object.assign(filters, emptyFilters())
}

const pageCount = computed(() => Math.max(1, Math.ceil(result.value.totalCount / PAGE_SIZE)))
const hasFilters = computed(() => Object.values(filters).some(Boolean))
const rangeLabel = computed(() => {
  const count = result.value.items.length
  if (count === 0) return 'Showing 0 of 0'
  const start = (page.value - 1) * PAGE_SIZE + 1
  return `Showing ${start}-${start + count - 1} of ${result.value.totalCount}`
})

watch(
  () => ({ ...filters }),
  () => {
    page.value = 1
    clearTimeout(timer)
    timer = setTimeout(load, 300)
  }
)

onMounted(load)

onBeforeUnmount(() => {
  clearTimeout(timer)
  controller?.abort()
})
</script>

<template>
  <div class="stack">
    <div>
      <h1>Leads</h1>
      <p class="muted">Totals and the list both follow the filters below.</p>
    </div>

    <p v-if="error" class="error" role="alert" data-testid="load-error">{{ error }}</p>

    <section class="tiles" aria-label="Totals by status">
      <div v-for="status in STATUSES" :key="status" class="tile">
        <div class="tile-label">
          <span class="dot" :style="{ background: STATUS_COLORS[status] }" aria-hidden="true"></span>
          <span>{{ status }}</span>
        </div>
        <div class="tile-count" :data-testid="`total-${status}`">{{ result.statusCounts[status] ?? 0 }}</div>
      </div>
    </section>

    <section class="card filters" aria-label="Filters">
      <div class="filter-grid">
        <div class="filter-search">
          <label class="label" for="filter-q">Search</label>
          <input id="filter-q" v-model="filters.q" class="field" type="search" placeholder="Name, company or email" data-testid="filter-search" />
        </div>
        <div>
          <label class="label" for="filter-status">Status</label>
          <select id="filter-status" v-model="filters.status" class="field" data-testid="filter-status">
            <option value="">All statuses</option>
            <option v-for="status in STATUSES" :key="status" :value="status">{{ status }}</option>
          </select>
        </div>
        <div>
          <label class="label" for="filter-source">Lead source</label>
          <select id="filter-source" v-model="filters.source" class="field" data-testid="filter-source">
            <option value="">All sources</option>
            <option v-for="source in SOURCES" :key="source.value" :value="source.value">{{ source.label }}</option>
          </select>
        </div>
        <div>
          <label class="label" for="filter-industry">Industry</label>
          <select id="filter-industry" v-model="filters.industry" class="field" data-testid="filter-industry">
            <option value="">All industries</option>
            <option v-for="industry in INDUSTRIES" :key="industry" :value="industry">{{ industry }}</option>
          </select>
        </div>
        <div>
          <label class="label" for="filter-owner">Owner</label>
          <select id="filter-owner" v-model="filters.owner" class="field" data-testid="filter-owner">
            <option value="">All owners</option>
            <option v-for="owner in OWNERS" :key="owner" :value="owner">{{ owner }}</option>
            <option value="Unassigned">Unassigned</option>
          </select>
        </div>
        <div>
          <label class="label" for="filter-from">Created from</label>
          <input id="filter-from" v-model="filters.from" class="field" type="date" data-testid="filter-from" />
        </div>
        <div>
          <label class="label" for="filter-to">Created to</label>
          <input id="filter-to" v-model="filters.to" class="field" type="date" data-testid="filter-to" />
        </div>
      </div>
      <button v-if="hasFilters" class="btn" type="button" data-testid="clear-filters" @click="clearFilters">Clear filters</button>
    </section>

    <section class="card" aria-label="Lead list">
      <div class="table-box">
        <table>
          <thead>
            <tr>
              <th scope="col">Lead</th>
              <th scope="col">Company</th>
              <th scope="col">Status</th>
              <th scope="col">Lead source</th>
              <th scope="col">Industry</th>
              <th scope="col">Owner</th>
              <th scope="col" class="right">Est. value</th>
              <th scope="col">Created</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="lead in result.items" :key="lead.id" data-testid="lead-row">
              <td>
                <div class="strong">{{ lead.firstName }} {{ lead.lastName }}</div>
                <div class="sub" data-testid="lead-email">{{ lead.email }}</div>
              </td>
              <td>
                <div>{{ lead.company }}</div>
                <div class="sub">{{ lead.jobTitle ?? DASH }}</div>
              </td>
              <td>
                <div class="status">
                  <span class="dot" :style="{ background: STATUS_COLORS[lead.status] }" aria-hidden="true"></span>
                  <span>{{ lead.status }}</span>
                </div>
              </td>
              <td>{{ sourceLabel(lead.source) }}</td>
              <td>{{ lead.industry ?? DASH }}</td>
              <td>{{ lead.owner ?? 'Unassigned' }}</td>
              <td class="right mono">{{ lead.estimatedValue === null ? DASH : money.format(lead.estimatedValue) }}</td>
              <td class="mono">{{ dates.format(new Date(lead.createdAt)) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <p v-if="!loading && result.items.length === 0" class="empty" data-testid="no-results">No leads match these filters.</p>

      <div class="pager">
        <span data-testid="pager-range">{{ rangeLabel }}</span>
        <div class="pager-buttons">
          <button class="btn" type="button" :disabled="page <= 1" data-testid="page-prev" @click="goToPage(page - 1)">Previous</button>
          <span>Page {{ page }} of {{ pageCount }}</span>
          <button class="btn" type="button" :disabled="page >= pageCount" data-testid="page-next" @click="goToPage(page + 1)">Next</button>
        </div>
      </div>
    </section>
  </div>
</template>
