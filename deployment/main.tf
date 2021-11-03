terraform {
  backend "azurerm" {
    resource_group_name  = "azbrokerterraformrg"
    storage_account_name = "azbrokerbackend"
    container_name       = "azbrokerterraformstate"
  }
}

data "terraform_remote_state" "broker_persistent" {
  backend = "azurerm"
  config = {
    storage_account_name = "azbrokerbackend"
    container_name       = "azbrokerterraformstate"
    key                  = "dev/broker-persistent.tfstate"
    access_key           = var.state_access_key
  }
}

locals {
  app_service_name = "netsbrokerdemo${var.environment_name == "test" ? "" : var.environment_name}"
  app_hostname    = "https://${local.app_service_name}.azurewebsites.net"
}

provider "azurerm" {
  version = "=1.36.0"

  // See https://www.terraform.io/docs/providers/azurerm/guides/service_principal_client_secret.html
  // Got subscription_id through `az account list`
  // Got the others from `az ad sp create-for-rbac --role="Contributor" --scopes="/subscriptions/d554e6fd-3fe6-445e-ba3f-096fe4937a91"
}

resource "azurerm_resource_group" "intragration-client" {
  name     = "Integration-demo-${var.environment_name}"
  location = var.location
}

resource "azurerm_app_service_plan" "intragration-client" {
  name                = "SP-Integrationdemo-${var.environment_name}"
  kind                = "linux"
  reserved            = true
  location            = azurerm_resource_group.intragration-client.location
  resource_group_name = azurerm_resource_group.intragration-client.name

  sku {
    tier = "Basic"
    size = "B1"
  }
}

resource "azurerm_app_service" "integration-client" {
  name                = local.app_service_name
  location            = azurerm_resource_group.intragration-client.location
  resource_group_name = azurerm_resource_group.intragration-client.name
  app_service_plan_id = azurerm_app_service_plan.intragration-client.id

  site_config {
    linux_fx_version = "DOCKER|${data.terraform_remote_state.broker_persistent.outputs.docker_registry}/signaturgruppen/broker/integration-client:${var.broker_service_imagetag}"
    always_on        = "true"
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    WEBSITES_ENABLE_APP_SERVICE_STORAGE         = false
    DOCKER_REGISTRY_SERVER_URL                  = "https://${data.terraform_remote_state.broker_persistent.outputs.docker_registry}"
    DOCKER_REGISTRY_SERVER_USERNAME             = "${data.terraform_remote_state.broker_persistent.outputs.docker_username}"
    DOCKER_REGISTRY_SERVER_PASSWORD             = "${data.terraform_remote_state.broker_persistent.outputs.docker_accesskey}"
    OPHost                                      = "https//broker${var.environment_name}"
    WEBSITES_PORT                               = "5014"
    BROKER_CLIENT_APPSETTINGS__AUTHORITY        = "https://broker${var.environment_name}.signaturgruppen.dk/op"
    BROKER_CLIENT_APPSETTINGS__CLIENTID         = var.openid_client_id
    BROKER_CLIENT_APPSETTINGS__CLIENTSECRET     = var.openid_client_secret
    BROKER_CLIENT_APPSETTINGS__CLIENTSIGNINGKEY = var.openid_request_signing_key
    BROKER_CLIENT_APPSETTINGS__APPLICATIONURL   = local.app_hostname

  }

}
