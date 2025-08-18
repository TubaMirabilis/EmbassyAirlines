terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
  }

  backend "s3" {
    bucket         = "a11y-online-tf-state"
    key            = "embassy-airlines-state"
    region         = "eu-west-2"
    encrypt        = true
    dynamodb_table = "ao-tf-state-lock"
  }
}