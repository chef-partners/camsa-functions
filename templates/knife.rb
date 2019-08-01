current_dir = ::File.dirname(__FILE__)
{{#newline}}
log_level                :info
{{#newline}}
log_location             $stdout
{{#newline}}
node_name                "{{user}}"
{{#newline}}
client_key               ::File.join(current_dir, "{{client_key_filename}}")
{{#newline}}
validation_client_name   "{{org}}-validator"
{{#newline}}
validation_key           ::File.join(current_dir, "{{org_key_filename}}")
{{#newline}}
chef_server_url          "{{chef_server_url}}"
{{#newline}}
cookbook_path            [::File.join(current_dir, "../cookbooks")]