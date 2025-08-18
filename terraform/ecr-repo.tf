module "web" {
  source                 = "github.com/welcome-ally-ltd/aws-ecr-repo"
  repo_name              = "embassy-web"
  force_delete           = true
  enable_lambda_function = false

}
