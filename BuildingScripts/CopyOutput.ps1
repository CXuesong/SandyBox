mkdir -Force $args[1]
Copy-Item -Recurse "$($args[0])\*" $args[1]