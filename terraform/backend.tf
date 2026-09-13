terraform {
  backend "s3" {
    bucket       = "qiglo-terraform-state-333058598913"
    key          = "monitoring/terraform.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}
