variable "location" {
  type    = string
  default = "westus"
}
variable "resource_group_name" {
  type    = string
  default = "rg-university-social"
}
variable "name_prefix" {
  type    = string
  default = "university-social"
}
variable "postgres_admin_login" {
  type    = string
  default = "universityadmin"
}
variable "postgres_admin_password" {
  type      = string
  sensitive = true
}
variable "jwt_key" {
  type      = string
  sensitive = true
}
variable "backend_image" {
  type    = string
  default = "mcr.microsoft.com/k8se/quickstart:latest"
}
variable "frontend_image" {
  type    = string
  default = "mcr.microsoft.com/k8se/quickstart:latest"
}
variable "container_target_port" {
  type    = number
  default = 80
}
variable "environment" {
  type    = string
  default = "production"
}
