module "ecr_repo" {
    source = "github.com/welcome-ally-ltd/aws-ecr-repo"
    repo_name = "example"
    force_delete = true 
    enable_lambda_function = false
    
}