variable "state_access_key" {
    description = "Access key to get remote state for data \"terraform_remote_state\""
}

variable "location" {
  description = "Azure location to use. Has to be one of centralus,eastasia,southeastasia,eastus,eastus2,westus,westus2,northcentralus,southcentralus,westcentralus,northeurope,westeurope,japaneast,japanwest,brazilsouth,australiasoutheast,australiaeast,westindia,southindia,centralindia,canadacentral,canadaeast,uksouth,ukwest,koreacentral,koreasouth,francecentral,southafricanorth,uaenorth,australiacentral"
  default = "northeurope"
  type = string
}

variable "broker_service_imagetag" {
  description = "Image tag to use"
  default = "0.2.0"
  type = string
}

variable "environment_name"{
  default = "dev"
  type = string
}

variable "openid_client_id" {
  description = "The client id that will make the OIDC requests"
  type = string
}

variable "openid_client_secret" {
  type = string
}

variable "openid_request_signing_key" {
  type = string
}

variable "authority" {
  type = string
  default = "https://brokerdev.signaturgruppen.dk/op"
}