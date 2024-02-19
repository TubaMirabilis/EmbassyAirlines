# Get complete array  of all image names using docker image ls
IMAGES=$(docker image ls --filter 'dangling=false' --format '{{.Repository}}')

# Loop through the array of image names
for IMAGE in $IMAGES
do
    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
    aquasec/trivy image --quiet --severity CRITICAL,HIGH $IMAGE 
done
