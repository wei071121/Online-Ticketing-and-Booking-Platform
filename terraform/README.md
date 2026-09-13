# QIGLO Terraform Monitoring

This Terraform configuration provisions a CloudWatch dashboard for the QIGLO Event Ticketing platform.

## Existing infrastructure referenced

- Application Load Balancer
- Auto Scaling Group
- ALB Target Group

## Terraform-managed resource

- ASG average CPU utilization widget
- Healthy and unhealthy target count widget
- ALB request count widget

The AWS provider is pinned to version 5.0.0 for the limited 8 GiB lab EC2 environment.
Terraform state, provider binaries and secret values are not committed.

## Remote state

Terraform state is stored in a private, encrypted and versioned S3 backend with native S3 state locking enabled.
