{{/*
Expand the chart name.
*/}}
{{- define "enterprise-agent-os.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "enterprise-agent-os.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{- define "enterprise-agent-os.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" -}}
{{- end -}}

{{- define "enterprise-agent-os.labels" -}}
helm.sh/chart: {{ include "enterprise-agent-os.chart" . }}
app.kubernetes.io/name: {{ include "enterprise-agent-os.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/part-of: enterprise-agent-os
{{- end -}}

{{- define "enterprise-agent-os.componentLabels" -}}
{{ include "enterprise-agent-os.labels" .root }}
app.kubernetes.io/component: {{ .component }}
{{- end -}}

{{- define "enterprise-agent-os.selectorLabels" -}}
app.kubernetes.io/name: {{ include "enterprise-agent-os.name" .root }}
app.kubernetes.io/instance: {{ .root.Release.Name }}
app.kubernetes.io/component: {{ .component }}
{{- end -}}

{{- define "enterprise-agent-os.componentName" -}}
{{- printf "%s-%s" (include "enterprise-agent-os.fullname" .root) .component | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "enterprise-agent-os.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
{{- default (include "enterprise-agent-os.fullname" .) .Values.serviceAccount.name -}}
{{- else -}}
{{- default "default" .Values.serviceAccount.name -}}
{{- end -}}
{{- end -}}

{{- define "enterprise-agent-os.secretName" -}}
{{- default (printf "%s-backend" (include "enterprise-agent-os.fullname" .)) .Values.secrets.existingSecret -}}
{{- end -}}

{{- define "enterprise-agent-os.imageTag" -}}
{{- default .root.Values.global.imageTag .tag -}}
{{- end -}}

