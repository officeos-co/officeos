The one thing worth knowing

This means any other pod in the default namespace on your nova cluster can
reach any zeroclaw pod with no auth. Today that's fine — kubectl get pods
-n default shows your own workloads only — but the moment you:

- Share the cluster with another tenant
- Grant namespace default access to a third party
- Deploy something to default that might get compromised (e.g. a webhook  
  receiver)

...that other workload could reach every zeroclaw pod directly. If you  
 want belt-and-suspenders, a NetworkPolicy locks it down with one YAML:

apiVersion: networking.k8s.io/v1  
 kind: NetworkPolicy  
 metadata:  
 name: zeroclaw-allow-backend-only  
 namespace: default  
 spec:  
 podSelector:  
 matchLabels:  
 app: zeroclaw  
 policyTypes: [Ingress]  
 ingress: - from: - podSelector:  
 matchLabels:
app: eaos-backend-prod  
 ports:

- protocol: TCP  
  port: 42617
