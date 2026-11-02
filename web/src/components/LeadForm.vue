<script setup>
import { reactive, ref } from 'vue'
import { createLead } from '../api/leads'
import { INDUSTRIES, OWNERS, SOURCES, STATUSES } from '../options'

const emit = defineEmits(['saved', 'cancel'])

const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  company: '',
  jobTitle: '',
  industry: '',
  status: 'New',
  source: '',
  owner: '',
  estimatedValue: ''
})

const errors = reactive({})
const formError = ref('')
const saving = ref(false)

function resetErrors() {
  Object.keys(errors).forEach((key) => delete errors[key])
  formError.value = ''
}

function validate() {
  const email = form.email.trim()
  const value = form.estimatedValue.trim()

  if (!form.firstName.trim()) errors.firstName = 'Enter a first name.'
  if (!form.lastName.trim()) errors.lastName = 'Enter a last name.'
  if (!email) errors.email = 'Enter an email.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) errors.email = 'Enter a valid email.'
  if (!form.company.trim()) errors.company = 'Enter a company.'
  if (!form.source) errors.source = 'Choose a lead source.'
  if (value && !(Number(value) >= 0)) errors.estimatedValue = 'Enter a positive number.'

  return Object.keys(errors).length === 0
}

function showServerErrors(result) {
  if (result.status === 409) {
    errors.email = result.problem?.title ?? 'A lead with this email already exists.'
    return
  }

  const serverErrors = Object.entries(result.problem?.errors ?? {})

  for (const [key, messages] of serverErrors) {
    const field = key.charAt(0).toLowerCase() + key.slice(1)
    if (field in form) errors[field] = messages[0]
  }

  if (Object.keys(errors).length === 0) {
    formError.value = 'The lead could not be saved. Try again.'
  }
}

async function submit() {
  resetErrors()
  if (!validate()) return

  saving.value = true

  try {
    const value = form.estimatedValue.trim()
    const result = await createLead({
      firstName: form.firstName,
      lastName: form.lastName,
      email: form.email.trim(),
      company: form.company,
      status: form.status,
      source: form.source,
      phone: form.phone || null,
      jobTitle: form.jobTitle || null,
      industry: form.industry || null,
      owner: form.owner || null,
      estimatedValue: value ? Number(value) : null
    })

    if (result.ok) {
      emit('saved', result.lead)
    } else {
      showServerErrors(result)
    }
  } catch {
    formError.value = 'Could not reach the API. Check that it is running.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="form-page">
    <h1>New lead</h1>
    <p class="muted">Fields marked * are required.</p>

    <form class="card form-card" novalidate data-testid="lead-form" @submit.prevent="submit">
      <p v-if="formError" class="error" role="alert" data-testid="form-error">{{ formError }}</p>

      <fieldset>
        <legend>Contact</legend>
        <div class="form-grid">
          <div>
            <label class="label" for="lead-first">First name *</label>
            <input id="lead-first" v-model="form.firstName" class="field" type="text" autocomplete="off" data-testid="lead-firstName" @input="delete errors.firstName" />
            <p v-if="errors.firstName" class="field-error" data-testid="error-firstName">{{ errors.firstName }}</p>
          </div>
          <div>
            <label class="label" for="lead-last">Last name *</label>
            <input id="lead-last" v-model="form.lastName" class="field" type="text" autocomplete="off" data-testid="lead-lastName" @input="delete errors.lastName" />
            <p v-if="errors.lastName" class="field-error" data-testid="error-lastName">{{ errors.lastName }}</p>
          </div>
          <div>
            <label class="label" for="lead-email">Email *</label>
            <input id="lead-email" v-model="form.email" class="field" type="email" autocomplete="off" data-testid="lead-email-input" @input="delete errors.email" />
            <p v-if="errors.email" class="field-error" data-testid="error-email">{{ errors.email }}</p>
          </div>
          <div>
            <label class="label" for="lead-phone">Phone</label>
            <input id="lead-phone" v-model="form.phone" class="field" type="tel" autocomplete="off" data-testid="lead-phone" />
          </div>
        </div>
      </fieldset>

      <fieldset>
        <legend>Company</legend>
        <div class="form-grid">
          <div>
            <label class="label" for="lead-company">Company *</label>
            <input id="lead-company" v-model="form.company" class="field" type="text" autocomplete="off" data-testid="lead-company" @input="delete errors.company" />
            <p v-if="errors.company" class="field-error" data-testid="error-company">{{ errors.company }}</p>
          </div>
          <div>
            <label class="label" for="lead-title">Job title</label>
            <input id="lead-title" v-model="form.jobTitle" class="field" type="text" autocomplete="off" data-testid="lead-jobTitle" />
          </div>
          <div>
            <label class="label" for="lead-industry">Industry</label>
            <select id="lead-industry" v-model="form.industry" class="field" data-testid="lead-industry">
              <option value="">Not set</option>
              <option v-for="industry in INDUSTRIES" :key="industry" :value="industry">{{ industry }}</option>
            </select>
          </div>
        </div>
      </fieldset>

      <fieldset>
        <legend>Sales details</legend>
        <div class="form-grid">
          <div>
            <label class="label" for="lead-status">Status *</label>
            <select id="lead-status" v-model="form.status" class="field" data-testid="lead-status">
              <option v-for="status in STATUSES" :key="status" :value="status">{{ status }}</option>
            </select>
          </div>
          <div>
            <label class="label" for="lead-source">Lead source *</label>
            <select id="lead-source" v-model="form.source" class="field" data-testid="lead-source" @change="delete errors.source">
              <option value="">Choose a source</option>
              <option v-for="source in SOURCES" :key="source.value" :value="source.value">{{ source.label }}</option>
            </select>
            <p v-if="errors.source" class="field-error" data-testid="error-source">{{ errors.source }}</p>
          </div>
          <div>
            <label class="label" for="lead-owner">Owner</label>
            <select id="lead-owner" v-model="form.owner" class="field" data-testid="lead-owner">
              <option value="">Unassigned</option>
              <option v-for="owner in OWNERS" :key="owner" :value="owner">{{ owner }}</option>
            </select>
          </div>
          <div>
            <label class="label" for="lead-value">Estimated value ($)</label>
            <input id="lead-value" v-model="form.estimatedValue" class="field" type="text" inputmode="decimal" autocomplete="off" data-testid="lead-estimatedValue" @input="delete errors.estimatedValue" />
            <p v-if="errors.estimatedValue" class="field-error" data-testid="error-estimatedValue">{{ errors.estimatedValue }}</p>
          </div>
        </div>
      </fieldset>

      <div class="form-actions">
        <button class="btn btn-primary" type="submit" :disabled="saving" data-testid="save-lead">{{ saving ? 'Saving...' : 'Save lead' }}</button>
        <button class="btn" type="button" data-testid="cancel-lead" @click="emit('cancel')">Cancel</button>
      </div>
    </form>
  </div>
</template>
