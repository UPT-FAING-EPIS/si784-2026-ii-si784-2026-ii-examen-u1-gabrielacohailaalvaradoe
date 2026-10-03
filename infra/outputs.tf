output "resource_group_name" {
  value = azurerm_resource_group.main.name
}
output "container_registry_name" {
  value = azurerm_container_registry.main.name
}
output "container_registry_login_server" {
  value = azurerm_container_registry.main.login_server
}
output "backend_url" {
  value = "https://${azurerm_container_app.backend.latest_revision_fqdn}"
}
output "frontend_url" {
  value = "https://${azurerm_container_app.frontend.latest_revision_fqdn}"
}
output "postgres_server" {
  value = azurerm_postgresql_flexible_server.main.fqdn
}
