<script setup>
import { ref } from 'vue'
import LeadDashboard from './components/LeadDashboard.vue'
import LeadForm from './components/LeadForm.vue'

const view = ref('dashboard')
const notice = ref('')

function showDashboard() {
  view.value = 'dashboard'
}

function showForm() {
  notice.value = ''
  view.value = 'form'
}

function leadSaved(lead) {
  notice.value = `${lead.firstName} ${lead.lastName} was added.`
  showDashboard()
}
</script>

<template>
  <header class="topbar">
    <div class="topbar-inner">
      <span class="brand">Leads Tracker</span>
      <nav class="nav" aria-label="Main">
        <button class="btn" type="button" data-testid="nav-dashboard" @click="showDashboard">Dashboard</button>
        <button class="btn btn-primary" type="button" data-testid="nav-new-lead" @click="showForm">New lead</button>
      </nav>
    </div>
  </header>
  <main class="page">
    <div v-if="view === 'dashboard'" class="stack">
      <p v-if="notice" class="notice" role="status" data-testid="notice">{{ notice }}</p>
      <LeadDashboard />
    </div>
    <LeadForm v-else @saved="leadSaved" @cancel="showDashboard" />
  </main>
</template>
