module "ecr_set" {
    source = "https://github.com/welcome-ally-ltd/aws-ecr-repo"
    repo_name = "test"
    force_delete = true 
    enable_lambda_function = false
    
}